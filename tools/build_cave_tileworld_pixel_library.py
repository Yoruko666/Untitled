"""Create the PixelCaveTilePreview module library in CaveTileWorld's actual palette.

Run once with Python 3. Copies the map's native 64px textures/material settings,
reuses proven module meshes, and saves a separate gallery + connected scene.
"""
from pathlib import Path
import re, shutil, uuid
import build_pixel_cave_connectors as author

ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets'
NAME='CaveTileWorldPixelTiles'
MATERIALS=ASSETS/'Art/Materials'/NAME
TEXTURES=ASSETS/'Art/Textures'/NAME
PREFABS=ASSETS/'Prefabs/Terrain'/NAME
CONFIGS=ASSETS/'Configs'/NAME
SCENE=ASSETS/'Scenes/CaveTileWorldPixelTilePreview.unity'


def asset_guid(path):
    return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]


def build():
    if SCENE.exists():
        raise RuntimeError('The library exists. Edit the saved assets; do not overwrite scene edits.')
    for folder in (MATERIALS,TEXTURES,PREFABS,CONFIGS):author.folder(folder)
    lookup={asset_guid(p):p for p in (ASSETS/'Art/Textures').rglob('*.png') if Path(str(p)+'.meta').exists()}
    texture_guids={}
    map_materials=ASSETS/'Art/Materials/CaveTerracedV2'
    for name in ('earth','rock','moss','wet','stone'):
        material=(map_materials/(name+'.mat')).read_text()
        source_guid=re.search(r'_BaseMap:\n        m_Texture: .*guid: (\w+)',material)[1]
        source=lookup[source_guid]
        path=TEXTURES/('cave_tileworld_'+name+'_64.png')
        shutil.copyfile(source,path)
        meta=Path(str(source)+'.meta').read_text()
        new_guid=uuid.uuid4().hex;texture_guids[source_guid]=new_guid
        meta=re.sub(r'^guid: \w+','guid: '+new_guid,meta,flags=re.M)
        meta=re.sub(r'enableMipMap: \d+','enableMipMap: 0',meta)
        meta=re.sub(r'filterMode: \d+','filterMode: 0',meta)
        meta=re.sub(r'textureCompression: \d+','textureCompression: 0',meta)
        meta=re.sub(r'maxTextureSize: \d+','maxTextureSize: 64',meta)
        author.write(Path(str(path)+'.meta'),meta)
    material_guids={}
    mapping={'earth':('earth',1),'stone':('stone',1),'moss':('moss',1),'wet':('wet',1),
             'rock':('rock',1),'rock_shadow':('rock',.72),'rock_light':('rock',1.12),'rim':('earth',.85)}
    for name,(reference,factor) in mapping.items():
        text=(map_materials/(reference+'.mat')).read_text()
        text=re.sub(r'  m_Name: .*','  m_Name: '+name,text,count=1)
        for old,new in texture_guids.items():text=text.replace(old,new)
        rgb=re.search(r'_BaseColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',text)
        values=tuple(float(v)*factor for v in rgb.groups())
        text=re.sub(r'    - _BaseColor: .*','    - _BaseColor: {r: %.6g, g: %.6g, b: %.6g, a: 1}'%values,text)
        path=MATERIALS/(name+'.mat');author.write(path,text)
        new=author.guid(path,'NativeFormatImporter',2100000)
        material_guids[asset_guid(ASSETS/'Art/Materials/PixelCaveTiles'/(name+'.mat'))]=new
    # Gallery pedestals are independent from map surface styling.
    pedestal_source=ASSETS/'Art/Materials/PixelCaveTiles/sample_pedestal.mat'
    path=MATERIALS/'sample_pedestal.mat';author.write(path,pedestal_source.read_text())
    material_guids[asset_guid(pedestal_source)]=author.guid(path,'NativeFormatImporter',2100000)
    prefab_guids={}
    for source in (ASSETS/'Prefabs/Terrain/PixelCaveTiles').glob('*.prefab'):
        text=source.read_text()
        for old,new in material_guids.items():text=text.replace(old,new)
        path=PREFABS/source.name;author.write(path,text)
        prefab_guids[asset_guid(source)]=author.guid(path,'PrefabImporter')
    config_guids={}
    sources=list((ASSETS/'Configs/PixelCaveTiles').glob('*.asset'))
    for source in sources:
        path=CONFIGS/source.name
        config_guids[asset_guid(source)]=author.guid(path,'NativeFormatImporter',11400000)
    for source in sources:
        text=source.read_text()
        for old,new in (prefab_guids|config_guids).items():text=text.replace(old,new)
        author.write(CONFIGS/source.name,text)
    scene=(ASSETS/'Scenes/PixelCaveTilePreview.unity').read_text()
    for old,new in (material_guids|prefab_guids|config_guids).items():scene=scene.replace(old,new)
    # Use the map's ambient/fog palette, retaining the library's review cameras/lights.
    map_scene=(ASSETS/'Scenes/CaveTileWorld.unity').read_text()
    render=re.search(r'--- !u!104 &2\n.*?(?=--- !u!)',map_scene,re.S)[0]
    render=re.sub(r'  m_Sun: .*','  m_Sun: {fileID: 0}',render)
    scene=re.sub(r'--- !u!104 &2\n.*?(?=--- !u!)',lambda _:render,scene,flags=re.S)
    scene=scene.replace('PIXEL CAVE / TILE LIBRARY','CAVE TILE WORLD / PIXEL LIBRARY')
    scene=scene.replace('01 Tile library','01 CaveTileWorld color tile library')
    scene=scene.replace('02 Connected tile sample','02 CaveTileWorld connected tile sample')
    scene=show_connected_camera(scene)
    author.write(SCENE,scene);author.guid(SCENE,'DefaultImporter')
    print('Saved:',SCENE.relative_to(ROOT))
    print(len(prefab_guids),'prefabs;',len(material_guids),'materials;',len(texture_guids),'native map textures;',len(config_guids),'configs/presets')


def show_connected_camera(scene):
    # Default Game view shows the actual connected assembly, not just individual tiles.
    blocks=re.split(r'(?=^--- !u!)',scene,flags=re.M)
    names={re.search(r'^--- !u!1 &(\d+)',b,re.M)[1]:re.search(r'  m_Name: (.*)',b)[1]
           for b in blocks if b.startswith('--- !u!1 ')}
    for i,b in enumerate(blocks):
        if not b.startswith('--- !u!20 '):continue
        owner=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]
        name=names[owner]
        if name in ('Tile Library Camera','Assembly Camera'):
            blocks[i]=re.sub(r'  m_Enabled: \d+','  m_Enabled: '+str(int(name=='Assembly Camera')),b,count=1)
    return ''.join(blocks)


if __name__=='__main__':build()
