"""Author cave wall, entrance and hollow passage modules in Unity's native format.

Run: python tools/build_pixel_cave_walls.py
XZ ground, Y up, floor-level origins. Reuses the map-matched 64px materials.
"""
import math
from pathlib import Path
import build_pixel_cave_connectors as author

ROOT = Path(__file__).resolve().parents[1]
author.MODELS = ROOT / 'Assets/Art/Models/PixelCaveWalls'
author.PREFABS = ROOT / 'Assets/Prefabs/Terrain/PixelCaveWalls'
Mesh = author.Mesh


def extrude_xy(mesh, polygon, front, back, face='rock', edge='rock_shadow'):
    """Convex CCW XY cross-section, extruded along Z; caps face away from volume."""
    a=[(x,y,front) for x,y in polygon]
    b=[(x,y,back) for x,y in polygon]
    for i in range(1,len(polygon)-1):
        mesh.face([a[0],a[i+1],a[i]],face)
        mesh.face([b[0],b[i],b[i+1]],face)
    for i in range(len(polygon)):
        j=(i+1)%len(polygon)
        mesh.face([a[i],a[j],b[j],b[i]],edge)


def wall(mesh, height=4.8, moss=False, broken=False):
    first_face=len(mesh.faces)
    xs=[-2,-1,0,1,2]
    # Boundary cross-sections stay identical, including the low-wall variant.
    peaks=[height,height-.32,height-.12,height-.46,height]
    if broken:
        peaks=[height,height*.82,height*.78,height*.90,height]
    levels=[0,height*.38,height*.75]
    bulges=[0,.2,-.08,.14,0]
    for i in range(4):
        bands=[]
        for k in range(4):
            row=[]
            for j in (i,i+1):
                y=levels[k] if k<3 else peaks[j]
                front=-1 if j in (0,4) else -1-bulges[j]+(.22 if k==3 else 0)
                row.append(((xs[j],y,front),(xs[j],y,1)))
            bands.append(row)
        for k in range(3):
            a,b=bands[k]; c,d=bands[k+1]
            mesh.face([a[0],b[0],d[0],c[0]],'rock' if k!=1 else 'rock_shadow')
            mesh.face([b[1],a[1],c[1],d[1]],'rock')
            if i==0: mesh.face([a[1],a[0],c[0],c[1]],'rock')
            if i==3: mesh.face([b[0],b[1],d[1],d[0]],'rock')
        a,b=bands[0]
        mesh.face([b[0],a[0],a[1],b[1]],'rock_shadow')
        a,b=bands[-1]
        mesh.face([a[0],b[0],b[1],a[1]],'moss' if moss else 'rock_light')
    # The strips above trace the inner side of the shell; flip to outward normals.
    mesh.faces[first_face:]=[(list(reversed(points)),material) for points,material in mesh.faces[first_face:]]


def entrance(mesh, width=4, radius=1.3, spring=2.4, height=4.8, depth=2, blocked=False):
    half=width/2
    # Side posts leave a clear central opening all the way to the floor.
    mesh.box(-half,-radius,-depth/2,depth/2,height=height,bottom=0,top='rock_light',side='rock')
    mesh.box(radius,half,-depth/2,depth/2,height=height,bottom=0,top='rock_light',side='rock')
    # Faceted curved soffit. No triangles or hidden collider close the opening.
    count=12
    for i in range(count):
        a=math.pi-math.pi*i/count
        b=math.pi-math.pi*(i+1)/count
        x0,y0=radius*math.cos(a),spring+radius*math.sin(a)
        x1,y1=radius*math.cos(b),spring+radius*math.sin(b)
        extrude_xy(mesh,[(x0,y0),(x1,y1),(x1,height),(x0,height)],-depth/2,depth/2)
    if blocked:
        # Visible rock plugs, with matching collision; this entrance is intentionally impassable.
        for x,z,rise,radius in [(-.65,-.1,2.6,.9),(.65,.25,2.9,.95),(0,-.4,3.8,.85)]:
            pillar(mesh,x,z,radius,rise)


def pillar(mesh,x=0,z=0,radius=.9,height=4.8):
    count=7
    # Clockwise footprint and a tapered, fractured middle ring.
    base=[(x+radius*math.cos(-i*2*math.pi/count),0,z+radius*math.sin(-i*2*math.pi/count)) for i in range(count)]
    mid=[(x+radius*.72*math.cos(-i*2*math.pi/count),height*.62,z+radius*.72*math.sin(-i*2*math.pi/count)) for i in range(count)]
    top=[(x+radius*.56*math.cos(-i*2*math.pi/count),height-(.13 if i%2 else 0),z+radius*.56*math.sin(-i*2*math.pi/count)) for i in range(count)]
    for ring1,ring2 in [(base,mid),(mid,top)]:
        for i in range(count):
            j=(i+1)%count
            mesh.face([ring1[i],ring1[j],ring2[j],ring2[i]],'rock' if i%3 else 'rock_shadow')
    for i in range(1,count-1):
        mesh.face([base[0],base[i+1],base[i]],'rock_shadow')
        mesh.face([top[0],top[i],top[i+1]],'rock_light')


def save(mesh,name):
    mesh.save('tile_'+name)


def build():
    author.folder(author.MODELS)
    author.folder(author.PREFABS)
    for name,height,moss,broken in [('wall_straight_480',4.8,False,False),
                                    ('wall_moss_480',4.8,True,False),
                                    ('wall_broken_480',4.8,False,True),
                                    ('wall_low_240',2.4,False,False)]:
        m=Mesh(); wall(m,height,moss,broken); save(m,name)
    m=Mesh()
    m.box(-2,2,-2,0,height=4.8,bottom=0,top='rock_light')
    m.box(0,2,0,2,height=4.8,bottom=0,top='rock_light')
    save(m,'wall_inner_corner')
    m=Mesh()
    # Curved outer bend with matching two-metre-wide radial end faces.
    for i in range(6):
        a=i*math.pi/12; b=(i+1)*math.pi/12
        p=[(-2+2*math.cos(a),-2+2*math.sin(a)),
           (-2+2*math.cos(b),-2+2*math.sin(b)),
           (-2+4*math.cos(b),-2+4*math.sin(b)),
           (-2+4*math.cos(a),-2+4*math.sin(a))]
        m.prism(p,4.8,0,'rock_light','rock')
    save(m,'wall_outer_corner')
    m=Mesh()
    m.prism([(-1,-1),(-1,1),(.5,1),(1,.5),(1,-.5),(.5,-1)],4.8,0,'rock_light','rock')
    save(m,'wall_end')
    m=Mesh()
    # Back face attaches to a wall; front narrows to a low supporting foot.
    extrude_xy(m,[(-1,0),(1,0),(.65,3.6),(-.65,3.6)],-1.5,.5)
    save(m,'wall_buttress')
    for name,args in [('entrance_small',{}),('entrance_wide',{'width':8,'radius':2.8,'spring':2.4,'height':6.4}),
                      ('entrance_blocked',{'blocked':True}),('tunnel_straight',{'depth':4})]:
        m=Mesh(); entrance(m,**args); save(m,name)
    m=Mesh(); entrance(m,depth=2)
    m.box(-2,2,.75,1,height=4.8,bottom=0,top='rock_light')
    save(m,'wall_alcove')
    m=Mesh()
    # South-to-east L passage. The opening is 2.6m wide, ceiling clear at 4.4m.
    m.box(-4,-3.3,-4,4,height=4.4,bottom=0,top='rock_light')
    m.box(-3.3,4,-.7,4,height=4.4,bottom=0,top='rock_light')
    m.box(-.7,4,-4,-3.3,height=4.4,bottom=0,top='rock_light')
    m.box(-4,4,-4,4,height=4.8,bottom=4.4,top='rock_light',side='rock')
    save(m,'tunnel_turn')
    m=Mesh(); m.box(-2,2,-2,2,height=4.8,bottom=4.4,top='rock_light',side='rock_shadow'); save(m,'roof_480')
    m=Mesh(); pillar(m); save(m,'rock_pillar_480')
    m=Mesh()
    # Sloping rock brow above the passage, leaving 3.2m clear headroom at its tip.
    extrude_xy(m,[(-2,3.2),(2,3.2),(2,4.8),(-2,4.8)],-.5,1)
    wedge=Mesh()
    extrude_xy(wedge,[(-1.5,3.2),(-.5,3.2),(-.5,4.8)],-2,2)
    # Exchange X/Z axes; the reflection requires reversing each face winding.
    for points,material in wedge.faces:
        m.face([(z,y,x) for x,y,z in reversed(points)],material)
    save(m,'rock_overhang')


if __name__=='__main__':
    build()
