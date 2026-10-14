#!/usr/bin/env python3
"""Compile runtime sources under debug gates; inspect emitted metadata. Not a Player build.

Run after Unity has compiled the current project. Uses only the bundled Unity compiler,
cached runtime references and stdlib; does not change PlayerSettings or overwrite assemblies.
"""
import argparse
from pathlib import Path
import subprocess


def main():
    project = Path(__file__).resolve().parents[1]
    version = (project / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-contents", type=Path,
                        default=Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents"))
    args = parser.parse_args()
    unity = args.unity_contents
    dotnet = unity / "NetCoreRuntime/dotnet"
    compiler = unity / "DotNetSdkRoslyn/csc.dll"
    mono = unity / "MonoBleedingEdge/bin/mono"
    cecil = unity / "Managed/Unity.Cecil.dll"
    for required in (dotnet, compiler, mono, cecil):
        if not required.is_file():
            raise SystemExit(f"Missing Unity dependency: {required}")
    candidates = list((project / "Library/Bee/artifacts").glob("*/Assembly-CSharp.rsp"))
    if not candidates:
        raise SystemExit("Compile the project in Unity first: Assembly-CSharp.rsp missing.")
    response = max(candidates, key=lambda p: p.stat().st_mtime).read_text().splitlines()
    if not any("DebugCommandService.cs" in line for line in response):
        raise SystemExit("Unity compiler response is stale; import/compile the new files first.")
    output = project / "Library/DeveloperToolsVerification/Compilation"
    output.mkdir(parents=True, exist_ok=True)
    base = []
    for line in response:
        if line.startswith(("-out:", "-refout:", "/additionalfile:")):
            continue
        if line.startswith("-define:"):
            symbol = line.split(":", 1)[1]
            if symbol.startswith("UNITY_EDITOR") or symbol in ("UNITY_INCLUDE_TESTS", "DEVELOPMENT_BUILD", "UNTITLED_DEVELOPER_TOOLS"):
                continue
        if line.startswith("-r:") and "editor" in Path(line.split(":", 1)[1].strip('"')).name.lower():
            continue
        base.append(line)

    variants = {
        "release": [],
        "release-optin-symbol": ["UNTITLED_DEVELOPER_TOOLS"],
        "development-default": ["DEVELOPMENT_BUILD"],
        "development-optin": ["DEVELOPMENT_BUILD", "UNTITLED_DEVELOPER_TOOLS"],
    }
    for name, defines in variants.items():
        rsp = output / f"{name}.rsp"
        rsp.write_text("\n".join(base + [f'-out:"{output / (name + ".dll")}"']
                                + [f"-define:{symbol}" for symbol in defines]) + "\n")
        result = subprocess.run([str(dotnet), str(compiler), "@" + str(rsp)], cwd=project,
                                text=True, capture_output=True, timeout=120)
        (output / f"{name}.log").write_text(result.stdout + result.stderr)
        if result.returncode:
            raise SystemExit(f"Compilation failed for {name}:\n{result.stdout}{result.stderr}")

    # Cecil reads metadata without loading gameplay types or resolving Editor-built dependencies.
    source = output / "InspectDebugGates.cs"
    source.write_text('''using System;
using System.Linq;
using Mono.Cecil;
class InspectDebugGates
{
    static int Main(string[] args)
    {
        string[] types = { "DebugCommandService", "DebugCommandRequest", "DebugCommandResult", "DebugManager", "DebugOverlay" };
        foreach (string name in args)
        {
            bool enabled = name.EndsWith("development-optin.dll");
            using (var assembly = AssemblyDefinition.ReadAssembly(name))
            {
                foreach (string type in types)
                    if (assembly.MainModule.Types.Any(t => t.FullName == "Untitled." + type) != enabled)
                        throw new Exception("Unexpected debug type: " + type + " in " + name);
                bool saveGuard = assembly.MainModule.Types.Single(t => t.FullName == "Untitled.GameManager")
                    .Methods.Any(m => m.Name == "MarkDeveloperStateModified");
                if (saveGuard != enabled) throw new Exception("Unexpected save guard in " + name);
                if (assembly.MainModule.AssemblyReferences.Any(r => r.Name.ToLowerInvariant().Contains("editor")))
                    throw new Exception("Runtime assembly references Editor in " + name);
                Console.WriteLine(System.IO.Path.GetFileName(name) + ": compile + metadata PASSED; debug types=" + enabled);
            }
        }
        return 0;
    }
}
''')
    inspector = output / "InspectDebugGates.exe"
    mono_compiler = unity / "MonoBleedingEdge/lib/mono/4.5/csc.exe"
    subprocess.run([str(mono), str(mono_compiler), "/nologo", "/out:" + str(inspector),
                    "/r:" + str(cecil), str(source)], check=True, cwd=project, timeout=30)
    # Place only the inspection dependency next to its executable; all artifacts stay in Library.
    (output / cecil.name).write_bytes(cecil.read_bytes())
    result = subprocess.run([str(mono), str(inspector)] + [str(output / f"{name}.dll") for name in variants],
                            text=True, capture_output=True, cwd=project, timeout=30)
    (output / "metadata.txt").write_text(result.stdout + result.stderr)
    print(result.stdout, end="")
    if result.returncode:
        raise SystemExit(result.stderr)
    print("DEVELOPER_TOOLS_COMPILATION_GATES_PASSED (cached dependencies; no Player packaging)")


if __name__ == "__main__":
    main()
