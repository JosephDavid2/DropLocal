using System.Text;

public static class PairQr
{
    const int N = 37;
    public static (bool[,] Pixels, bool[,] Reserved) Layout()
    {
        var p = new bool[N,N]; var f = new bool[N,N];
        void Set(int x,int y,bool b) { if(x>=0&&y>=0&&x<N&&y<N){p[y,x]=b;f[y,x]=true;} }
        for(int i=0;i<N;i++){Set(6,i,i%2==0);Set(i,6,i%2==0);}
        foreach(var (cx,cy) in new[]{(3,3),(33,3),(3,33)}) for(int dy=-4;dy<=4;dy++)for(int dx=-4;dx<=4;dx++){int d=Math.Max(Math.Abs(dx),Math.Abs(dy));Set(cx+dx,cy+dy,d!=2&&d!=4);}
        for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)Set(30+dx,30+dy,Math.Max(Math.Abs(dx),Math.Abs(dy))!=1);
        const int format=0x77C4;
        for(int i=0;i<=5;i++)Set(8,i,((format>>i)&1)!=0);
        Set(8,7,((format>>6)&1)!=0);Set(8,8,((format>>7)&1)!=0);Set(7,8,((format>>8)&1)!=0);
        for(int i=9;i<15;i++)Set(14-i,8,((format>>i)&1)!=0);
        for(int i=0;i<8;i++)Set(N-1-i,8,((format>>i)&1)!=0);
        for(int i=8;i<15;i++)Set(8,N-15+i,((format>>i)&1)!=0);
        Set(8,N-8,true); return(p,f);
    }
    public static bool[,] Encode(string value)
    {
        var bytes=Encoding.UTF8.GetBytes(value); if(bytes.Length>106)throw new ArgumentException("Dados longos demais para o QR.");
        var bits=new List<bool>(); void Push(int val,int count){for(int i=count-1;i>=0;i--)bits.Add(((val>>i)&1)!=0);}
        Push(4,4);Push(bytes.Length,8);foreach(byte b in bytes)Push(b,8);Push(0,Math.Min(4,864-bits.Count));while(bits.Count%8!=0)bits.Add(false);
        var data=new List<byte>();for(int i=0;i<bits.Count;i+=8){int b=0;for(int j=0;j<8;j++)b=b*2+(bits[i+j]?1:0);data.Add((byte)b);}
        for(int i=0;data.Count<108;i++)data.Add((byte)(i%2==0?0xEC:0x11));
        static byte Mul(int a,int b){int r=0;while(b>0){if((b&1)!=0)r^=a;a<<=1;if((a&256)!=0)a^=0x11D;b>>=1;}return(byte)r;}
        var divisor=new byte[26];divisor[^1]=1;byte root=1;
        for(int i=0;i<26;i++){for(int j=0;j<26;j++){divisor[j]=Mul(divisor[j],root);if(j+1<26)divisor[j]^=divisor[j+1];}root=Mul(root,2);}
        var ecc=new byte[26];foreach(byte b in data){byte factor=(byte)(b^ecc[0]);Array.Copy(ecc,1,ecc,0,25);ecc[^1]=0;for(int j=0;j<26;j++)ecc[j]^=Mul(divisor[j],factor);}
        data.AddRange(ecc);var (p,f)=Layout();int index=0;
        for(int right=N-1;right>=1;right-=2){if(right==6)right=5;for(int v=0;v<N;v++){int y=((right+1)&2)==0?N-1-v:v;for(int j=0;j<2;j++){int x=right-j;if(f[y,x])continue;bool b=index<data.Count*8&&((data[index/8]>>(7-index%8))&1)!=0;index++;p[y,x]=b^((x+y)%2==0);}}}
        return p;
    }
}
