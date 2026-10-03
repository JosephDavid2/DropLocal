package com.droplocal;
import java.nio.file.*;
import java.util.*;
public class QrRobustnessCheck {
 public static void main(String[] args)throws Exception {
  List<String> lines=Files.readAllLines(Path.of(args[0]));String expected=lines.get(0);boolean[][] original=new boolean[37][37];for(int y=0;y<37;y++)for(int x=0;x<37;x++)original[y][x]=lines.get(y+1).charAt(x)=='1';
  List<int[]> coordinates=new ArrayList<>();boolean[][] reserved=PairQrDecoder.reserved();for(int right=36;right>=1;right-=2){if(right==6)right=5;for(int v=0;v<37;v++){int y=((right+1)&2)==0?36-v:v;for(int j=0;j<2;j++){int x=right-j;if(!reserved[y][x])coordinates.add(new int[]{x,y});}}}
  Random random=new Random(4873);for(int errors=1;errors<=13;errors++)for(int trial=0;trial<20;trial++){boolean[][] matrix=new boolean[37][];for(int y=0;y<37;y++)matrix[y]=original[y].clone();Set<Integer> used=new HashSet<>();while(used.size()<errors)used.add(random.nextInt(134));for(int word:used){int mask=1+random.nextInt(255);for(int bit=0;bit<8;bit++)if((mask&(1<<bit))!=0){int[] c=coordinates.get(word*8+bit);matrix[c[1]][c[0]]^=true;}}if(!expected.equals(PairQrDecoder.decode(matrix)))throw new Exception("RS failed: "+errors+" errors trial "+trial);}
  System.out.println("PASS RS: 260 randomized damaged blocks, 1–13 bytes corrected");
  boolean[][] excessive=new boolean[37][];for(int y=0;y<37;y++)excessive[y]=original[y].clone();for(int i=0;i<20;i++){int[] c=coordinates.get(i*8);excessive[c[1]][c[0]]^=true;}if(PairQrDecoder.decode(excessive)!=null)throw new Exception("Excessive corruption accepted");System.out.println("PASS uncorrectable damage rejected");
  int count=0;for(String line:Files.readAllLines(Path.of(args[1],"manifest.txt"))){String[] parts=line.split(" ");int w=Integer.parseInt(parts[1]),h=Integer.parseInt(parts[2]);byte[] pixels=Files.readAllBytes(Path.of(args[1],parts[0]));long start=System.nanoTime();PairQrDecoder.ScanResult result=PairQrDecoder.scanDetailed(pixels,w,h);boolean shouldRead=parts[3].equals("read");if(shouldRead?!expected.equals(result.value):result.value!=null)throw new Exception("Frame failed: "+line+"; finders="+result.finders+" sampled="+result.sampled);System.out.printf("PASS %-30s %dms (%s)%n",parts[0],(System.nanoTime()-start)/1000000,shouldRead?"decoded":"rejected");count++;}System.out.println("PASS "+count+" optical fixtures");
 }
}
