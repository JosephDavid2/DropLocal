import java.nio.file.*;
import java.util.*;
import com.droplocal.PairQrDecoder;
public class QrDecoderCheck {
 public static void main(String[] args)throws Exception{
  List<String> lines=Files.readAllLines(Path.of(args[0]));String expected=lines.get(0);boolean[][] matrix=new boolean[37][37];for(int y=0;y<37;y++)for(int x=0;x<37;x++)matrix[y][x]=lines.get(y+1).charAt(x)=='1';
  if(!expected.equals(PairQrDecoder.decode(matrix)))throw new Exception("Matrix decoding failed");
  matrix[36][36]=!matrix[36][36];if(!expected.equals(PairQrDecoder.decode(matrix)))throw new Exception("Single-byte correction failed");matrix[36][36]=!matrix[36][36];System.out.println("PASS QR single-byte Reed-Solomon correction");
  int w=640,h=480;byte[] frame=new byte[w*h];Arrays.fill(frame,(byte)235);int originX=185,originY=105;
  for(int y=0;y<37;y++)for(int x=0;x<37;x++)if(matrix[y][x])for(int dy=0;dy<6;dy++)for(int dx=0;dx<6;dx++)frame[(originY+(y+4)*6+dy)*w+originX+(x+4)*6+dx]=16;
  for(int rotation=0;rotation<4;rotation++){String actual=PairQrDecoder.scan(frame,w,h);if(!expected.equals(actual))throw new Exception("Camera frame rotation "+rotation+" failed: "+actual);System.out.println("PASS QR live-frame decoding rotation "+(rotation*90));byte[] rotated=new byte[frame.length];for(int y=0;y<h;y++)for(int x=0;x<w;x++)rotated[x*h+(h-1-y)]=frame[y*w+x];frame=rotated;int swap=w;w=h;h=swap;}
 }
}
