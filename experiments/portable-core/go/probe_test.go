package probe

import (
 "encoding/csv"
 "math"
 "os"
 "path/filepath"
 "strconv"
 "testing"
)
func fixture(t *testing.T, name string)[][]string{
 t.Helper()
 f,err:=os.Open(filepath.Join("..","fixtures",name));if err!=nil{t.Fatal(err)}
 defer f.Close()
 rows,err:=csv.NewReader(f).ReadAll();if err!=nil{t.Fatal(err)}
 return rows[1:]
}
func number(t *testing.T,s string)float64{
 t.Helper();v,e:=strconv.ParseFloat(s,64);if e!=nil{t.Fatal(e)};return v
}
func integer(t *testing.T,s string)int{
 t.Helper();v,e:=strconv.Atoi(s);if e!=nil{t.Fatal(e)};return v
}
func TestSharedSweepFixtures(t *testing.T){
 for _,row:=range fixture(t,"sweep-v1.csv"){
  plan:=Sweep{StartHz:number(t,row[0]),EndHz:number(t,row[1]),Seconds:integer(t,row[2]),Ear:Left}
  got,err:=plan.FrequencyAt(uint64(integer(t,row[3])));if err!=nil{t.Fatal(err)}
  want:=number(t,row[4])
  if math.Abs(got-want)>0.00001{t.Fatalf("sweep %+v frame %s: %g vs %g",plan,row[3],got,want)}
 }
}
func TestSharedNotchFixtures(t *testing.T){
 for _,r:=range fixture(t,"notch-v1.csv"){
  m,err:=MeasureNotch(number(t,r[0]),number(t,r[1]),integer(t,r[2]));if err!=nil{t.Fatal(err)}
  for i,pair:=range [][2]float64{{m.LowerHz,number(t,r[3])},{m.UpperHz,number(t,r[4])},{m.WidthOctaves,number(t,r[5])}} {
   limit:=0.0001; if i==2{limit=0.000001}
   if math.Abs(pair[0]-pair[1])>limit{t.Fatalf("notch %s dim %d: %.12f vs %.12f",r[0],i,pair[0],pair[1])}
  }
 }
}
func TestInvalidStopEarAndBlockInvariance(t *testing.T){
 invalid:=[]Sweep{
  {StartHz:100,EndHz:8000,Seconds:10},{StartHz:500,EndHz:500,Seconds:10},
  {StartHz:500,EndHz:8000,Seconds:120},{StartHz:math.NaN(),EndHz:8000,Seconds:10},
  {StartHz:500,EndHz:8000,Seconds:10,Ear:99},
 }
 for _,v:=range invalid{if _,err:=NewTone(v);err==nil{t.Fatalf("accepted invalid %+v",v)}}
 if _,err:=MeasureNotch(10000,0.5,16000);err==nil{t.Fatal("accepted beyond Nyquist")}
 s:=Sweep{StartHz:500,EndHz:8000,Seconds:8,Ear:Left}
 a,_:=NewTone(s); b,_:=NewTone(s)
 whole:=make([]int16,2048);split:=make([]int16,2048)
 if err:=a.Render(whole);err!=nil{t.Fatal(err)}
 for off:=0;off<len(split);off+=254{if err:=b.Render(split[off:off+254]);err!=nil{t.Fatal(err)}}
 nonzero:=false
 for i,v:=range whole{
  if v!=split[i]{t.Fatalf("chunk dependence at %d",i)}
  if math.Abs(float64(v))>2622{t.Fatalf("ceiling exceeded at %d",i)}
  if i%2==1&&v!=0{t.Fatal("right ear leakage")}
  if i%2==0&&v!=0{nonzero=true}
 }
 if !nonzero{t.Fatal("selected ear silent")}
 b.Stop();b.Stop()
 if err:=b.Render(split);err!=nil{t.Fatal(err)}
 for _,v:=range split{if v!=0{t.Fatal("Stop did not silence output")}}
 if err:=b.Render([]int16{1});err==nil{t.Fatal("accepted misaligned buffer")}
 // End is hard-bounded: finished handles cannot revive themselves.
 c,_:=NewTone(s); block:=make([]int16,8192)
 for n:=uint64(0);n<s.Frames();n+=uint64(len(block)/2){if err:=c.Render(block);err!=nil{t.Fatal(err)}}
 if err:=c.Render(block);err!=nil{t.Fatal(err)}
 for _,v:=range block{if v!=0{t.Fatal("continued after limit")}}
}
func TestNoRenderHeapAlloc(t *testing.T){
 s:=Sweep{StartHz:500,EndHz:8000,Seconds:15,Ear:Both};tone,_:=NewTone(s)
 out:=make([]int16,512)
 if got:=testing.AllocsPerRun(100,func(){if err:=tone.Render(out);err!=nil{panic(err)}});got!=0{
  t.Fatalf("unexpected render allocations: %.2f",got)
 }
}
func BenchmarkToneRender1024(b *testing.B){
 tone,_:=NewTone(Sweep{StartHz:500,EndHz:8000,Seconds:15,Ear:Both})
 dst:=make([]int16,2048);b.ReportAllocs();b.ResetTimer()
 for i:=0;i<b.N;i++ {if err:=tone.Render(dst);err!=nil{b.Fatal(err)}}
}
