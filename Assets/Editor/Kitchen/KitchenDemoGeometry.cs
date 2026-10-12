using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Untitled.Editor
{
    public static partial class KitchenDemoBuilder
    {
        private sealed class Geometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly SortedDictionary<int,List<int>> triangles = new SortedDictionary<int,List<int>>();
            public IEnumerable<int> UsedMaterials => triangles.Keys;

            private void Face(Vector3[] points, Finish finish, Vector3 outward)
            {
                points=points.Distinct().ToArray();
                if(points.Length<3) return;
                Vector3 normal=Vector3.zero;
                for(int i=1;i<points.Length-1;i++)
                {
                    Vector3 candidate=Vector3.Cross(points[i]-points[0],points[i+1]-points[0]);
                    if(candidate.sqrMagnitude>1e-12f){normal=candidate.normalized;break;}
                }
                if(normal.sqrMagnitude<0.5f)return;
                if(Vector3.Dot(normal,outward)<0){Array.Reverse(points);normal=-normal;}
                if(normal.sqrMagnitude<0.5f) normal=outward.normalized;
                int start=vertices.Count;
                foreach(Vector3 p in points)
                {
                    vertices.Add(p);normals.Add(normal);
                    Vector3 a=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));
                    uv.Add(a.y>=a.x&&a.y>=a.z?new Vector2(p.x,p.z)*0.55f:a.x>a.z?new Vector2(p.z,p.y)*0.55f:new Vector2(p.x,p.y)*0.55f);
                }
                if(!triangles.TryGetValue((int)finish,out var indices)){indices=new List<int>();triangles[(int)finish]=indices;}
                for(int i=1;i<points.Length-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);}
            }

            public void Box(Vector3 center,Vector3 size,Finish finish,Quaternion rotation,float bevel=0.025f)
            {
                float x=size.x/2,z=size.z/2,y=size.y/2;
                float b=Mathf.Min(bevel,Mathf.Min(x,Mathf.Min(z,y))*0.7f);
                Vector2[] outline={new Vector2(-x+b,-z),new Vector2(x-b,-z),new Vector2(x,-z+b),new Vector2(x,z-b),new Vector2(x-b,z),new Vector2(-x+b,z),new Vector2(-x,z-b),new Vector2(-x,-z+b)};
                Vector3 P(int ring,int i)
                {
                    Vector2 p=outline[i%8];float h=ring==0?-y:ring==1?-y+b:ring==2?y-b:y;
                    if(ring==0||ring==3) p=new Vector2(Mathf.Clamp(p.x,-x+b,x-b),Mathf.Clamp(p.y,-z+b,z-b));
                    return center+rotation*V(p.x,h,p.y);
                }
                for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++)
                {
                    var points=new[]{P(ring,i),P(ring,(i+1)%8),P(ring+1,(i+1)%8),P(ring+1,i)};
                    Face(points,finish,((points[0]+points[1]+points[2]+points[3])/4-center).normalized);
                }
                Face(Enumerable.Range(0,8).Select(i=>P(3,i)).ToArray(),finish,rotation*Vector3.up);
                Face(Enumerable.Range(0,8).Select(i=>P(0,i)).ToArray(),finish,rotation*Vector3.down);
            }

            public void Beam(Vector3 from,Vector3 to,float width,Finish finish)
            {
                Vector3 delta=to-from;
                Box((from+to)*0.5f,V(width,delta.magnitude,width),finish,Quaternion.FromToRotation(Vector3.up,delta),width*0.12f);
            }

            public void Lathe(Vector3 origin,Vector3[] profile,Finish finish,int sides,Quaternion? rotation=null)
            {
                Quaternion q=rotation??Quaternion.identity;
                Vector3 P(int j,int i){float a=i*Mathf.PI*2/sides;return origin+q*V(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x);}
                for(int j=0;j<profile.Length-1;j++)for(int i=0;i<sides;i++)
                {
                    float a=(i+0.5f)*Mathf.PI*2/sides;
                    float dr=profile[j+1].x-profile[j].x,dy=profile[j+1].y-profile[j].y;
                    Vector3 outward=q*V(dy*Mathf.Cos(a),-dr,dy*Mathf.Sin(a));
                    Face(new[]{P(j,i),P(j,i+1),P(j+1,i+1),P(j+1,i)},finish,outward);
                }
                Face(Enumerable.Range(0,sides).Select(i=>P(0,i)).ToArray(),finish,q*Vector3.down);
                Face(Enumerable.Range(0,sides).Select(i=>P(profile.Length-1,i)).ToArray(),finish,q*Vector3.up);
            }

            public void Ring(Vector3 origin,float outer,float inner,float height,Finish finish,int sides,Quaternion? rotation=null)
            {
                Quaternion q=rotation??Quaternion.identity;
                Vector3 P(float r,float h,int i){float a=i*Mathf.PI*2/sides;return origin+q*V(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r);}
                for(int i=0;i<sides;i++)
                {
                    float a=(i+0.5f)*Mathf.PI*2/sides;Vector3 n=q*V(Mathf.Cos(a),0,Mathf.Sin(a));
                    Face(new[]{P(outer,0,i),P(outer,0,i+1),P(outer,height,i+1),P(outer,height,i)},finish,n);
                    Face(new[]{P(inner,0,i),P(inner,height,i),P(inner,height,i+1),P(inner,0,i+1)},finish,-n);
                    Face(new[]{P(inner,height,i),P(outer,height,i),P(outer,height,i+1),P(inner,height,i+1)},finish,q*Vector3.up);
                    Face(new[]{P(inner,0,i),P(outer,0,i),P(outer,0,i+1),P(inner,0,i+1)},finish,q*Vector3.down);
                }
            }

            public void Rock(Vector3 center,Vector3 size,Finish finish,float seed)
            {
                const int sides=9;
                Vector3 P(int ring,int i)
                {
                    float a=i*Mathf.PI*2/sides;
                    float r=(ring==0?0.68f:ring==1?1:0.59f)*(1+0.10f*Mathf.Sin(i*2.4f+seed));
                    return center+Vector3.Scale(V(Mathf.Cos(a)*r*0.5f,ring==0?-0.4f:ring==1?0:0.34f,Mathf.Sin(a)*r*0.5f),size);
                }
                for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++)
                {
                    Vector3[] p={P(ring,i),P(ring,i+1),P(ring+1,i+1),P(ring+1,i)};
                    Face(p,finish,(p[0]+p[1]+p[2]+p[3])/4-center);
                }
                Vector3 peak=center+V(size.x*0.07f,size.y*0.5f,-size.z*0.06f);
                for(int i=0;i<sides;i++)Face(new[]{P(2,i),P(2,i+1),peak},finish,Vector3.up+(P(2,i)-center).normalized);
                Face(Enumerable.Range(0,sides).Select(i=>P(0,i)).ToArray(),finish,Vector3.down);
            }

            public Mesh ToMesh(string name)
            {
                var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=triangles.Count;
                int index=0;foreach(var indices in triangles.Values)mesh.SetTriangles(indices,index++);
                mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
