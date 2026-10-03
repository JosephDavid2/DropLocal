package com.droplocal;
import java.util.*;
import java.nio.charset.StandardCharsets;

public final class PairQrDecoder {
    static boolean[][] reserved(){boolean[][] f=new boolean[37][37];for(int i=0;i<37;i++){f[i][6]=true;f[6][i]=true;}for(int[] c:new int[][]{{3,3},{33,3},{3,33}})for(int y=-4;y<=4;y++)for(int x=-4;x<=4;x++)if(c[0]+x>=0&&c[0]+x<37&&c[1]+y>=0&&c[1]+y<37)f[c[1]+y][c[0]+x]=true;for(int y=28;y<=32;y++)for(int x=28;x<=32;x++)f[y][x]=true;for(int i=0;i<=5;i++)f[i][8]=true;f[7][8]=f[8][8]=f[8][7]=true;for(int i=9;i<15;i++)f[8][14-i]=true;for(int i=0;i<8;i++)f[8][36-i]=true;for(int i=8;i<15;i++)f[37-15+i][8]=true;f[29][8]=true;return f;}
    public static String decode(boolean[][] cells){boolean[][] f=reserved();ArrayList<Boolean> bits=new ArrayList<>();for(int right=36;right>=1;right-=2){if(right==6)right=5;for(int v=0;v<37;v++){int y=((right+1)&2)==0?36-v:v;for(int j=0;j<2;j++){int x=right-j;if(!f[y][x])bits.add(cells[y][x]^((x+y)%2==0));}}}byte[] words=new byte[134];for(int i=0;i<134;i++)for(int j=0;j<8;j++)words[i]=(byte)((words[i]<<1)|(bits.get(i*8+j)?1:0));
        if(!correct(words))return null;
        bits.clear();for(byte word:words)for(int j=7;j>=0;j--)bits.add(((word>>j)&1)!=0);
        int mode=0,len=0;for(int i=0;i<4;i++)mode=mode*2+(bits.get(i)?1:0);if(mode!=4)return null;for(int i=4;i<12;i++)len=len*2+(bits.get(i)?1:0);if(len<1||len>106)return null;byte[] bytes=new byte[len];for(int i=0;i<len;i++)for(int j=0;j<8;j++)bytes[i]=(byte)((bytes[i]<<1)|(bits.get(12+i*8+j)?1:0));String value=new String(bytes,StandardCharsets.UTF_8);return value.startsWith("droplocal://pair?ip=")&&value.matches("droplocal://pair\\?ip=[0-9.]+&port=45833&token=[0-9]{8}")?value:null;}
    static int mul(int a,int b){int result=0;while(b>0){if((b&1)!=0)result^=a;a<<=1;if((a&256)!=0)a^=0x11D;b>>=1;}return result;}
    static int power(int a,int n){int r=1;for(int i=0;i<n;i++)r=mul(r,a);return r;}
    static int divide(int a,int b){if(b==0)throw new IllegalArgumentException("zero divisor");return mul(a,power(b,254));}
    static int[] syndromes(byte[] words){int[] s=new int[26];int root=1;for(int i=0;i<26;i++){for(byte word:words)s[i]=mul(s[i],root)^(word&255);root=mul(root,2);}return s;}
    // Berlekamp-Massey locator + Chien search; solve error magnitudes over GF(256).
    // Version 5-L has one 134-byte block with 26 parity bytes (up to 13 corrupt bytes).
    static boolean correct(byte[] words){int[] s=syndromes(words);boolean clean=true;for(int v:s)clean&=v==0;if(clean)return true;
        int[] c=new int[27],b=new int[27];c[0]=b[0]=1;int length=0,shift=1,previous=1;
        for(int n=0;n<26;n++){int d=s[n];for(int i=1;i<=length;i++)d^=mul(c[i],s[n-i]);if(d==0){shift++;continue;}int[] old=c.clone();int factor=divide(d,previous);for(int i=0;i+shift<c.length;i++)c[i+shift]^=mul(factor,b[i]);if(2*length<=n){length=n+1-length;b=old;previous=d;shift=1;}else shift++;}
        if(length<1||length>13)return false;ArrayList<Integer> positions=new ArrayList<>();ArrayList<Integer> locations=new ArrayList<>();
        for(int pos=0;pos<words.length;pos++){int location=power(2,words.length-1-pos),inverse=divide(1,location),v=c[length];for(int i=length-1;i>=0;i--)v=mul(v,inverse)^c[i];if(v==0){positions.add(pos);locations.add(location);}}
        if(positions.size()!=length)return false;int[][] system=new int[length][length+1];for(int r=0;r<length;r++){for(int col=0;col<length;col++)system[r][col]=power(locations.get(col),r);system[r][length]=s[r];}
        for(int col=0;col<length;col++){int pivot=col;while(pivot<length&&system[pivot][col]==0)pivot++;if(pivot==length)return false;int[] swap=system[pivot];system[pivot]=system[col];system[col]=swap;int inv=divide(1,system[col][col]);for(int j=col;j<=length;j++)system[col][j]=mul(system[col][j],inv);for(int r=0;r<length;r++)if(r!=col){int f=system[r][col];for(int j=col;j<=length;j++)system[r][j]^=mul(f,system[col][j]);}}
        for(int i=0;i<length;i++)words[positions.get(i)]^=(byte)system[i][length];for(int v:syndromes(words))if(v!=0)return false;return true;
    }
    static class Point {double x,y,m;int votes=1;Point(double x,double y,double m){this.x=x;this.y=y;this.m=m;}}
    static boolean black(byte[] pixels,int w,int h,int x,int y){return x>=0&&y>=0&&x<w&&y<h&&(pixels[y*w+x]&255)<128;}
    static boolean ratio(int[] runs){int total=0;for(int n:runs)total+=n;if(total<14)return false;double m=total/7.0;for(int i=0;i<5;i++)if(Math.abs(runs[i]-m*(i==2?3:1))>m*(i==2?1.3:0.7))return false;return true;}
    static Point vertical(byte[] p,int w,int h,int x,int y,double m){int[] runs=new int[5];int up=y;while(up>=0&&black(p,w,h,x,up)){runs[2]++;up--;}while(up>=0&&!black(p,w,h,x,up)&&runs[1]<m*3){runs[1]++;up--;}while(up>=0&&black(p,w,h,x,up)&&runs[0]<m*3){runs[0]++;up--;}int down=y+1;while(down<h&&black(p,w,h,x,down)){runs[2]++;down++;}while(down<h&&!black(p,w,h,x,down)&&runs[3]<m*3){runs[3]++;down++;}while(down<h&&black(p,w,h,x,down)&&runs[4]<m*3){runs[4]++;down++;}if(!ratio(runs))return null;double cy=down-runs[4]-runs[3]-runs[2]/2.0;return new Point(x,cy,m);}
    public static final class ScanResult {public String value;public int finders,contrast;public boolean sampled;}
    public static String scan(byte[] p,int w,int h){return scanDetailed(p,w,h).value;}
    public static ScanResult scanDetailed(byte[] pixels,int w,int h){if(w<40||h<40||pixels.length<w*h)throw new IllegalArgumentException("Quadro de câmera incompleto");ScanResult result=new ScanResult();
        int[] histogram=new int[256];for(int i=0;i<w*h;i+=4)histogram[pixels[i]&255]++;int count=0;for(int v:histogram)count+=v;int low=0,high=255,sum=0;while(low<255&&(sum+=histogram[low])<count/20)low++;sum=0;while(high>0&&(sum+=histogram[high])<count/20)high--;result.contrast=high-low;
        scanBinary(adaptive(pixels,w,h),w,h,result);if(result.value!=null)return result;
        // Global Otsu is a useful fallback when a tiny QR falls inside low-contrast tiles.
        double total=0;for(int i=0;i<256;i++)total+=(double)i*histogram[i];int back=0,threshold=128;double backSum=0,best=-1;
        for(int i=0;i<255;i++){back+=histogram[i];backSum+=(double)i*histogram[i];if(back==0||back==count)continue;double diff=backSum/back-(total-backSum)/(count-back);double score=(double)back*(count-back)*diff*diff;if(score>best){best=score;threshold=i;}}
        byte[] binary=new byte[w*h];for(int i=0;i<binary.length;i++)binary[i]=(byte)((pixels[i]&255)<=threshold?0:255);scanBinary(binary,w,h,result);return result;
    }
    static byte[] adaptive(byte[] p,int w,int h){int columns=(w+7)/8,rows=(h+7)/8;int[][] points=new int[rows][columns];
        for(int by=0;by<rows;by++)for(int bx=0;bx<columns;bx++){int min=255,max=0,sum=0,n=0;for(int y=by*8;y<Math.min(h,by*8+8);y++)for(int x=bx*8;x<Math.min(w,bx*8+8);x++){int v=p[y*w+x]&255;min=Math.min(min,v);max=Math.max(max,v);sum+=v;n++;}int point=sum/n;if(max-min<24){point=min/2;if(by>0&&bx>0){int neighbor=(points[by-1][bx]+2*points[by][bx-1]+points[by-1][bx-1])/4;if(min<neighbor)point=neighbor;}}points[by][bx]=point;}
        byte[] out=new byte[w*h];for(int by=0;by<rows;by++)for(int bx=0;bx<columns;bx++){int sum=0,n=0;for(int yy=Math.max(0,by-2);yy<=Math.min(rows-1,by+2);yy++)for(int xx=Math.max(0,bx-2);xx<=Math.min(columns-1,bx+2);xx++){sum+=points[yy][xx];n++;}int threshold=sum/n;for(int y=by*8;y<Math.min(h,by*8+8);y++)for(int x=bx*8;x<Math.min(w,bx*8+8);x++)out[y*w+x]=(byte)((p[y*w+x]&255)<=threshold?0:255);}return out;
    }
    static void scanBinary(byte[] p,int w,int h,ScanResult result){ArrayList<Point> found=new ArrayList<>();for(int y=0;y<h;y+=2){ArrayList<Integer> lengths=new ArrayList<>(),starts=new ArrayList<>();ArrayList<Boolean> colors=new ArrayList<>();int start=0;boolean color=black(p,w,h,0,y);for(int x=1;x<=w;x++){boolean next=x<w&&black(p,w,h,x,y);if(x==w||next!=color){lengths.add(x-start);starts.add(start);colors.add(color);start=x;color=next;}}
            for(int i=0;i+4<lengths.size();i++){if(!colors.get(i))continue;int[] r=new int[5];int total=0;for(int j=0;j<5;j++){r[j]=lengths.get(i+j);total+=r[j];}if(!ratio(r))continue;double cx=starts.get(i)+r[0]+r[1]+r[2]/2.0;Point candidate=vertical(p,w,h,(int)cx,y,total/7.0);if(candidate==null)continue;boolean merged=false;for(Point old:found)if(Math.hypot(old.x-candidate.x,old.y-candidate.y)<Math.max(old.m,candidate.m)*2){old.x=(old.x*old.votes+candidate.x)/(old.votes+1);old.y=(old.y*old.votes+candidate.y)/(old.votes+1);old.votes++;merged=true;break;}if(!merged)found.add(candidate);}}
        found.removeIf(point->point.votes<2);result.finders=Math.max(result.finders,found.size());found.sort((a,b)->Integer.compare(b.votes,a.votes));if(found.size()>10)found=new ArrayList<>(found.subList(0,10));
        for(Point tl:found)for(Point a:found)for(Point b:found){if(tl==a||tl==b||a==b)continue;double ax=a.x-tl.x,ay=a.y-tl.y,bx=b.x-tl.x,by=b.y-tl.y,al=Math.hypot(ax,ay),bl=Math.hypot(bx,by);if(al<30||bl<30||Math.abs(ax*bx+ay*by)>0.55*al*bl||al/bl>2||bl/al>2)continue;result.sampled=true;
            // Try affine first; then use the lower-right alignment target to remove perspective.
            double[] affine={ax/30,bx/30,tl.x-3*(ax+bx)/30,ay/30,by/30,tl.y-3*(ay+by)/30,0,0};
            String value=sample(p,w,h,affine);if(value!=null){result.value=value;return;}
            Point alignment=alignment(p,w,h,tl,ax,ay,bx,by);if(alignment!=null){double[] transform=homography(new double[][]{{3,3,tl.x,tl.y},{33,3,a.x,a.y},{3,33,b.x,b.y},{30,30,alignment.x,alignment.y}});if(transform!=null){value=sample(p,w,h,transform);if(value!=null){result.value=value;return;}}}
        }
    }
    static String sample(byte[] p,int w,int h,double[] t){boolean[][] cells=new boolean[37][37];for(int y=0;y<37;y++)for(int x=0;x<37;x++){int votes=0;for(double[] offset:new double[][]{{0,0},{-.18,0},{.18,0},{0,-.18},{0,.18}}){double xx=x+offset[0],yy=y+offset[1],d=t[6]*xx+t[7]*yy+1;int sx=(int)Math.round((t[0]*xx+t[1]*yy+t[2])/d),sy=(int)Math.round((t[3]*xx+t[4]*yy+t[5])/d);if(sx<0||sy<0||sx>=w||sy>=h)return null;if(black(p,w,h,sx,sy))votes++;}cells[y][x]=votes>=3;}return decode(cells);}
    static Point alignment(byte[] p,int w,int h,Point tl,double ax,double ay,double bx,double by){double ex=tl.x+.9*(ax+bx),ey=tl.y+.9*(ay+by),module=Math.min(Math.hypot(ax,ay),Math.hypot(bx,by))/30;int radius=(int)Math.ceil(module*7),step=Math.max(1,(int)(module/3));Point best=null;double bestScore=-1;
        for(int y=(int)ey-radius;y<=ey+radius;y+=step)for(int x=(int)ex-radius;x<=ex+radius;x+=step){if(!black(p,w,h,x,y))continue;for(double scale:new double[]{.8,1,1.2}){int matches=0;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int sx=(int)Math.round(x+scale*(dx*ax+dy*bx)/30),sy=(int)Math.round(y+scale*(dx*ay+dy*by)/30);boolean expected=Math.max(Math.abs(dx),Math.abs(dy))!=1;if(black(p,w,h,sx,sy)==expected)matches++;}double score=matches-Math.hypot(x-ex,y-ey)/(module*16);if(matches>=24&&score>bestScore){bestScore=score;best=new Point(x,y,module);}}}return best;
    }
    static double[] homography(double[][] pairs){double[][] a=new double[8][9];for(int i=0;i<4;i++){double x=pairs[i][0],y=pairs[i][1],u=pairs[i][2],v=pairs[i][3];a[2*i]=new double[]{x,y,1,0,0,0,-u*x,-u*y,u};a[2*i+1]=new double[]{0,0,0,x,y,1,-v*x,-v*y,v};}
        for(int col=0;col<8;col++){int pivot=col;for(int r=col+1;r<8;r++)if(Math.abs(a[r][col])>Math.abs(a[pivot][col]))pivot=r;if(Math.abs(a[pivot][col])<1e-8)return null;double[] swap=a[pivot];a[pivot]=a[col];a[col]=swap;double d=a[col][col];for(int j=col;j<9;j++)a[col][j]/=d;for(int r=0;r<8;r++)if(r!=col){double factor=a[r][col];for(int j=col;j<9;j++)a[r][j]-=factor*a[col][j];}}double[] out=new double[8];for(int i=0;i<8;i++)out[i]=a[i][8];return out;}
}
