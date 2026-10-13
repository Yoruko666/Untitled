"""Reuse the exact stratified mating profiles from PixelCaveTilePreview.

Creates 2.4/4.8m rock cliff presets and 4.8m natural wall modules.
Only vertical vertex positions change; source 4m border profiles are preserved.
"""
from pathlib import Path
import math, re, struct
import build_pixel_cave_connectors as author

ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets'
MODELS=ASSETS/'Art/Models/PixelCaveConnectedRocks'
PREFABS=ASSETS/'Prefabs/Terrain/PixelCaveConnectedRocks'
CONFIGS=ASSETS/'Configs/PixelCaveConnectedRocks'
SOURCE=ASSETS/'Art/Models/PixelCaveTiles/meshes'
SOURCE_PREFABS=ASSETS/'Prefabs/Terrain/PixelCaveTiles'
ROLES=('fill','edge','outer_corner','inner_corner','double_corner')


def scale_mesh(name,source,height):
    text=(SOURCE/(source+'.asset')).read_text()
    factor=height/3.2
    vertices=[list(v) for v in struct.iter_unpack('<12f',bytes.fromhex(re.search(r'_typelessdata: (\w+)',text)[1]))]
    for v in vertices:
        v[1]*=factor
        if abs(v[1])<1e-5:v[1]=0
        n=[v[3],v[4]/factor,v[5]]
        length=math.sqrt(sum(x*x for x in n));v[3:6]=[x/length for x in n]
        t=[v[6],v[7]*factor,v[8]]
        length=math.sqrt(sum(x*x for x in t));v[6:9]=[x/length for x in t]
    text=re.sub(r'  m_Name: .*','  m_Name: '+name,text,count=1)
    text=re.sub(r'    _typelessdata: .*','    _typelessdata: '+b''.join(struct.pack('<12f',*v) for v in vertices).hex(),text)
    def bounds(section):
        first=int(re.search(r'firstVertex: (\d+)',section)[1]);count=int(re.search(r'vertexCount: (\d+)',section)[1])
        center,extent=author.bounds([v[:3] for v in vertices[first:first+count]])
        section=re.sub(r'm_Center: .*','m_Center: '+author.vector(center),section)
        return re.sub(r'm_Extent: .*','m_Extent: '+author.vector(extent),section)
    text=re.sub(r'  - serializedVersion: 2\n.*?(?=  - serializedVersion: 2\n|  m_Shapes:)',lambda m:bounds(m[0]),text,flags=re.S)
    center,extent=author.bounds([v[:3] for v in vertices])
    text=re.sub(r'  m_LocalAABB:\n.*?(?=  m_MeshUsageFlags:)',f'  m_LocalAABB:\n    m_Center: {author.vector(center)}\n    m_Extent: {author.vector(extent)}\n',text,flags=re.S)
    path=MODELS/(name+'.asset');author.write(path,text)
    mesh_guid=author.guid(path,'NativeFormatImporter',4300000)
    prefab=(SOURCE_PREFABS/(source+'.prefab')).read_text()
    prefab=re.sub(r'  m_Name: .*','  m_Name: '+name,prefab,count=1)
    prefab=re.sub(r'(m_Mesh: \{fileID: 4300000, guid: )\w+',lambda m:m[1]+mesh_guid,prefab)
    # Preserve submesh order. Replace earth/rim on exposed rock tops with rock_light.
    mats=ASSETS/'Art/Materials/PixelCaveTiles'
    for old in ('earth','rim'):
        old_guid=re.search(r'guid: (\w+)',(mats/(old+'.mat.meta')).read_text())[1]
        new_guid=re.search(r'guid: (\w+)',(mats/'rock_light.mat.meta').read_text())[1]
        prefab=prefab.replace(old_guid,new_guid)
    path=PREFABS/(name+'.prefab');author.write(path,prefab)
    prefab_guid=author.guid(path,'PrefabImporter')
    game_object=int(re.search(r'^--- !u!1 &(\d+)',prefab,re.M)[1])
    print(name,'source profile:',source,'height:',height)
    return prefab_guid,game_object


def build():
    for folder in (MODELS,PREFABS,CONFIGS):author.folder(folder)
    for height,label in ((2.4,'240'),(4.8,'480')):
        refs={}
        for role in ROLES:
            refs[role]=scale_mesh('tile_rock_cliff_'+role+'_'+label,'tile_cliff_'+role,height)
        preset=(ASSETS/'Configs/PixelCaveTiles/pixel_cliffs_earth.asset').read_text()
        preset=re.sub(r'  m_Name: .*','  m_Name: pixel_rock_cliffs_'+label,preset,count=1)
        keys={'fill':'fillTile','edge':'edgeTile','outer_corner':'cornerTile',
              'inner_corner':'invertedCornerTile','double_corner':'doubleInteriorCornerTile'}
        for role,key in keys.items():
            pg,go=refs[role]
            preset=re.sub(r'  DUALGRD_'+key+r': \{fileID: \d+, guid: \w+,\n    type: 3\}',
                          '  DUALGRD_'+key+': {fileID: '+str(go)+', guid: '+pg+',\n    type: 3}',preset)
        path=CONFIGS/('pixel_rock_cliffs_'+label+'.asset')
        author.write(path,preset);author.guid(path,'NativeFormatImporter',11400000)
    for role in ('straight','corner','end'):
        scale_mesh('tile_rock_wall_'+role+'_480','tile_wall_'+role,4.8)


if __name__=='__main__':build()
