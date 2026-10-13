"""Build native Unity meshes/prefabs for the saved terraced cave map.

Run with Python 3. Uses existing Unity YAML templates; never edits the map.
4m grid, XZ ground, +Z uphill, origin at the lower walking surface.
"""
from pathlib import Path
import math
import re
import struct
import uuid

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets'
MODELS = ASSETS / 'Art/Models/PixelCaveConnectors'
PREFABS = ASSETS / 'Prefabs/Terrain/PixelCaveConnectors'
MATERIALS = ASSETS / 'Art/Materials/PixelCaveTiles'
mesh_template = (ASSETS / 'Art/Models/PixelCaveTiles/meshes/tile_floor_earth.asset').read_text()
prefab_template = (ASSETS / 'Prefabs/Terrain/PixelCaveTiles/tile_floor_earth.prefab').read_text()


def write(path, text):
    path.write_text(text, encoding='utf-8', newline='\n')


def guid(path, importer, file_id=0):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]
    value = uuid.uuid4().hex
    native = f'  mainObjectFileID: {file_id}\n' if file_id else ''
    write(meta, f'fileFormatVersion: 2\nguid: {value}\n{importer}:\n  externalObjects: {{}}\n{native}  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    return value


def folder(path):
    path.mkdir(exist_ok=True)
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        write(meta, 'fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')


def subtract(a, b):
    return tuple(x-y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


class Mesh:
    def __init__(self):
        self.faces = []

    def face(self, points, material):
        self.faces.append((points, material))

    def prism(self, polygon, height, bottom=-.25, top='earth', side='rock'):
        # Clockwise XZ perimeter produces upward-facing caps in Unity's Y-up frame.
        upper = [(x, height(x,z) if callable(height) else height, z) for x,z in polygon]
        lower = [(x,bottom,z) for x,z in polygon]
        # Convex footprints only. Concave corners are made from adjoining rectangles.
        for i in range(1, len(polygon)-1):
            self.face([upper[0], upper[i], upper[i+1]], top)
            self.face([lower[0], lower[i+1], lower[i]], side)
        for i in range(len(polygon)):
            j = (i+1) % len(polygon)
            self.face([upper[i], lower[i], lower[j], upper[j]], side)

    def box(self, x0, x1, z0, z1, height=0, bottom=-.25, top='earth', side='rock'):
        self.prism([(x0,z0),(x0,z1),(x1,z1),(x1,z0)], height, bottom, top, side)

    def save(self, name):
        vertices = []
        indices = []
        slots = list(dict.fromkeys(material for _,material in self.faces))
        sections = []
        for slot in slots:
            first_byte = len(indices)*2
            first_vertex = len(vertices)
            for points, material in self.faces:
                if material != slot:
                    continue
                normal = cross(subtract(points[1],points[0]), subtract(points[2],points[0]))
                length = math.sqrt(sum(x*x for x in normal))
                assert length > 1e-7, (name, points)
                normal = tuple(x/length for x in normal)
                tangent = cross((0,0,1) if abs(normal[2]) < .9 else (0,1,0), normal)
                length = math.sqrt(sum(x*x for x in tangent))
                tangent = tuple(x/length for x in tangent)
                start = len(vertices)
                for point in points:
                    uv = (point[0]/4,point[2]/4) if abs(normal[1]) > .5 else (point[0]/4,point[1]/4)
                    vertices.append((*point,*normal,*tangent,1,*uv))
                for i in range(1,len(points)-1):
                    indices.extend((start,start+i,start+i+1))
            section_points = [v[:3] for v in vertices[first_vertex:]]
            center, extent = bounds(section_points)
            sections.append(f'  - serializedVersion: 2\n    firstByte: {first_byte}\n    indexCount: {len(indices)-first_byte//2}\n    topology: 0\n    baseVertex: 0\n    firstVertex: {first_vertex}\n    vertexCount: {len(vertices)-first_vertex}\n    localAABB:\n      m_Center: {vector(center)}\n      m_Extent: {vector(extent)}\n')
        text = re.sub(r'  m_Name: .*', '  m_Name: '+name, mesh_template, count=1)
        text = re.sub(r'  m_SubMeshes:\n.*?(?=  m_Shapes:)', '  m_SubMeshes:\n'+''.join(sections), text, flags=re.S)
        text = re.sub(r'  m_IndexBuffer: .*', '  m_IndexBuffer: '+struct.pack('<'+'H'*len(indices),*indices).hex(),text)
        text = re.sub(r'    m_VertexCount: \d+', f'    m_VertexCount: {len(vertices)}',text)
        data = b''.join(struct.pack('<12f',*v) for v in vertices)
        text = re.sub(r'    m_DataSize: \d+', f'    m_DataSize: {len(data)}',text)
        text = re.sub(r'    _typelessdata: .*', '    _typelessdata: '+data.hex(),text)
        center, extent = bounds([v[:3] for v in vertices])
        text = re.sub(r'  m_LocalAABB:\n.*?(?=  m_MeshUsageFlags:)', f'  m_LocalAABB:\n    m_Center: {vector(center)}\n    m_Extent: {vector(extent)}\n',text,flags=re.S)
        path = MODELS/(name+'.asset')
        write(path,text)
        mesh_guid = guid(path,'NativeFormatImporter',4300000)
        prefab = re.sub(r'  m_Name: .*', '  m_Name: '+name,prefab_template,count=1)
        prefab = re.sub(r'(m_Mesh: \{fileID: 4300000, guid: )\w+', lambda m:m[1]+mesh_guid,prefab)
        refs = ''.join('  - {fileID: 2100000, guid: '+re.search(r'guid: (\w+)',(MATERIALS/(s+'.mat.meta')).read_text())[1]+', type: 2}\n' for s in slots)
        prefab = re.sub(r'  m_Materials:\n.*?(?=  m_StaticBatchInfo:)', '  m_Materials:\n'+refs,prefab,flags=re.S)
        path = PREFABS/(name+'.prefab')
        write(path,prefab)
        guid(path,'PrefabImporter')
        assert all(0 <= i < len(vertices) for i in indices)
        assert len(data) == len(vertices)*48
        print(name, len(vertices), 'vertices', len(indices)//3, 'triangles', 'materials:',slots)


def bounds(points):
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    return tuple((a+b)/2 for a,b in zip(lo,hi)), tuple((b-a)/2 for a,b in zip(lo,hi))


def vector(v):
    return '{'+', '.join(f'{axis}: {value:.7g}' for axis,value in zip('xyz',v))+'}'


def build():
    folder(MODELS)
    folder(PREFABS)
    for kind in ('gravel','path'):
        m=Mesh(); m.box(-2,2,-2,2,top=kind); m.save('tile_floor_'+kind)
    m=Mesh(); m.box(-2,2,-1,1,bottom=-.65,top='gravel'); m.save('tile_bank_straight')
    # Quarter-circle outer edge. The opposite winding would make the cap face down.
    arc=[(-2+4*math.cos(i*math.pi/12),-2+4*math.sin(i*math.pi/12)) for i in range(7)]
    m=Mesh(); m.prism([(-2,-2),*reversed(arc)],0,-.65,'gravel'); m.save('tile_bank_outer_corner')
    m=Mesh(); m.box(-2,0,-2,2,bottom=-.65,top='gravel'); m.box(0,2,0,2,bottom=-.65,top='gravel'); m.save('tile_bank_inner_corner')
    for rise,length,label in ((.45,4,'045'),(1.2,4,'120'),(2.4,8,'240')):
        m=Mesh(); m.box(-2,2,-length/2,length/2,height=lambda x,z:(z+length/2)*rise/length,top='path'); m.save('tile_ramp_'+label)
    m=Mesh()
    for i in range(8):
        m.box(-2,2,-2+i*.5,-2+(i+1)*.5,height=(i+1)*.15,top='stone')
    m.save('tile_steps_120')
    m=Mesh(); m.box(-2,2,-2,2,top='planks',side='planks',bottom=-.18)
    for x in (-1.55,1.55):
        m.box(x-.15,x+.15,-2,2,height=-.18,bottom=-.48,top='planks',side='planks')
    m.save('tile_bridge_deck')
    m=Mesh(); m.box(-2,2,-2,0,top='planks',side='planks'); m.box(-2,2,0,2,top='path'); m.save('tile_bridge_landing')
    m=Mesh(); m.box(-2,2,-.5,.5,height=0,bottom=-1.2,top='stone'); m.save('tile_retaining_edge_120')


if __name__ == '__main__':
    build()
