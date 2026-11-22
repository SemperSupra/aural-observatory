// Package probe is an independent R11 Go qualification candidate, NOT Aural production DSP.
// It deliberately implements only bounded reference-tone sweeps and an RBJ notch
// frequency-response oracle. It does not output sound to a device or handle user data.
package probe

import (
 "errors"
 "math"
)

const SampleRate = 48000
const DigitalCeiling = 0.08

var ErrInvalid = errors.New("aural r11: invalid or out-of-range parameter")

type Ear uint8
const (
 Both Ear = iota
 Left
 Right
)
type Sweep struct {
 StartHz, EndHz float64
 Seconds int
 Ear Ear
}
func (s Sweep) Validate() error {
 if math.IsNaN(s.StartHz)||math.IsInf(s.StartHz,0)||math.IsNaN(s.EndHz)||math.IsInf(s.EndHz,0)||
  s.StartHz<200||s.StartHz>10000||s.EndHz<200||s.EndHz>10000||s.StartHz==s.EndHz||
  s.Seconds<8||s.Seconds>15||s.Ear>Right { return ErrInvalid }
 return nil
}
func (s Sweep) Frames() uint64 { return uint64(s.Seconds*SampleRate) }
func (s Sweep) FrequencyAt(frame uint64) (float64,error) {
 if err:=s.Validate();err!=nil{return 0,err}
 if frame>=s.Frames(){return 0,ErrInvalid}
 return s.StartHz*math.Exp(math.Log(s.EndHz/s.StartHz)*float64(frame)/float64(s.Frames()-1)),nil
}
type Tone struct {
 plan Sweep
 frame uint64
 phase float64
 stopped bool
}
// NewTone verifies the complete configuration before producing any sound-domain samples.
func NewTone(s Sweep)(*Tone,error) {
 if err:=s.Validate();err!=nil{return nil,err}
 return &Tone{plan:s},nil
}
func (t *Tone) Stop() { if t!=nil {t.stopped=true} }
// Render writes caller-owned interleaved PCM16. No device access or allocations are needed.
func (t *Tone) Render(dst []int16) error {
 if t==nil||len(dst)==0||len(dst)%2!=0||len(dst)>8192{return ErrInvalid}
 for i:=0;i<len(dst)/2;i++ {
  var sample int16
  if !t.stopped&&t.frame<t.plan.Frames() {
   hz,_:=t.plan.FrequencyAt(t.frame)
   t.phase+=2*math.Pi*hz/SampleRate
   if t.phase>=2*math.Pi {t.phase-=2*math.Pi}
   ramp:=math.Min(1,float64(t.frame)/(0.1*SampleRate))
   fade:=math.Min(1,float64(t.plan.Frames()-t.frame)/(0.1*SampleRate))
   sample=int16(math.Round(math.Sin(t.phase)*DigitalCeiling*32767*ramp*fade))
  }
  if t.stopped||t.frame>=t.plan.Frames(){ sample=0 }
  if t.plan.Ear==Both||t.plan.Ear==Left {dst[2*i]=sample}else{dst[2*i]=0}
  if t.plan.Ear==Both||t.plan.Ear==Right {dst[2*i+1]=sample}else{dst[2*i+1]=0}
  t.frame++
 }
 return nil
}
type NotchMeasurement struct{ LowerHz,UpperHz,WidthOctaves float64 }
func MeasureNotch(centerHz,widthOctaves float64,sampleRate int)(NotchMeasurement,error) {
 if sampleRate<8000||sampleRate>384000||math.IsNaN(centerHz)||math.IsInf(centerHz,0)||
  centerHz<=0||centerHz>=float64(sampleRate)/2||math.IsNaN(widthOctaves)||
  math.IsInf(widthOctaves,0)||widthOctaves<0.1||widthOctaves>2 {return NotchMeasurement{},ErrInvalid}
 r:=math.Pow(2,widthOctaves)
 q:=math.Sqrt(r)/(r-1)
 w0:=2*math.Pi*centerHz/float64(sampleRate)
 alpha:=math.Sin(w0)/(2*q)
 b1:=-2*math.Cos(w0)
 a0:=1+alpha
 a1:=b1
 a2:=1-alpha
 magnitude:=func(hz float64)float64{
  w:=2*math.Pi*hz/float64(sampleRate)
  re:=1+b1*math.Cos(w)+math.Cos(2*w)
  im:=-b1*math.Sin(w)-math.Sin(2*w)
  dre:=a0+a1*math.Cos(w)+a2*math.Cos(2*w)
  dim:=-a1*math.Sin(w)-a2*math.Sin(2*w)
  return math.Sqrt((re*re+im*im)/(dre*dre+dim*dim))
 }
 target:=math.Sqrt(0.5)
 low,high:=0.,centerHz
 for i:=0;i<64;i++ {mid:=(low+high)/2;if magnitude(mid)>target{low=mid}else{high=mid}}
 lower:=(low+high)/2
 low,high=centerHz,float64(sampleRate)/2
 for i:=0;i<64;i++ {mid:=(low+high)/2;if magnitude(mid)<target{low=mid}else{high=mid}}
 upper:=(low+high)/2
 return NotchMeasurement{lower,upper,math.Log2(upper/lower)},nil
}
