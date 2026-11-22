/* Synthetic conformance only: no physical audio. Run from repository root. */
#include "aural_r11_kernel.h"
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

static void check(int condition, const char *what) {
    if (!condition) { fprintf(stderr,"FAIL %s\n",what); exit(1); }
}
static void fixtures(void) {
    FILE *f=fopen("experiments/portable-core/fixtures/sweep-v1.csv","r");
    check(f!=NULL,"sweep fixture accessible");
    char line[256];
    check(fgets(line,sizeof line,f)!=NULL,"sweep header");
    unsigned count=0;
    while(fgets(line,sizeof line,f)) {
        aural_r11_tone_config c={sizeof c,AURAL_R11_API_VERSION,0,0,0,AURAL_R11_LEFT};
        unsigned long long frame=0;
        double expected=0;
        check(sscanf(line,"%lf,%lf,%u,%llu,%lf",&c.start_hz,&c.end_hz,&c.seconds,
                     &frame,&expected)==5,"sweep row parse");
        double got=0;
        check(aural_r11_tone_frequency(&c,(uint64_t)frame,&got)==AURAL_R11_OK,"sweep compute");
        check(fabs(got-expected)<0.00001,"sweep frozen numerical tolerance");
        ++count;
    }
    fclose(f);
    check(count==13,"13 sweep fixtures");
    f=fopen("experiments/portable-core/fixtures/notch-v1.csv","r");
    check(f!=NULL,"notch fixture accessible");
    check(fgets(line,sizeof line,f)!=NULL,"notch header");
    count=0;
    while(fgets(line,sizeof line,f)) {
        double center=0,width=0,low=0,high=0,measured=0;
        unsigned sr=0;
        check(sscanf(line,"%lf,%lf,%u,%lf,%lf,%lf",&center,&width,&sr,
                     &low,&high,&measured)==6,"notch row parse");
        aural_r11_notch_result o={sizeof o,0,0,0};
        check(aural_r11_measure_notch(center,width,sr,&o)==AURAL_R11_OK,"notch compute");
        check(fabs(o.lower_hz-low)<0.0001 &&
              fabs(o.upper_hz-high)<0.0001 &&
              fabs(o.measured_width_octaves-measured)<0.000001,
              "notch frozen numerical tolerance");
        ++count;
    }
    fclose(f);
    check(count==4,"4 notch fixtures");
    puts("PASS: 13 sweep and 4 notch frozen cross-language fixtures");
}
static void tone(void) {
    aural_r11_tone_config c={sizeof c,AURAL_R11_API_VERSION,500,8000,8,AURAL_R11_LEFT};
    aural_r11_tone *one=NULL,*pieces=NULL;
    check(aural_r11_api_version()==1,"ABI version");
    check(aural_r11_tone_create(&c,&one)==AURAL_R11_OK,"create one");
    check(aural_r11_tone_create(&c,&pieces)==AURAL_R11_OK,"create pieces");
    int16_t full[2048]={0},chunked[2048]={0};
    check(aural_r11_tone_render(one,full,1024,2048)==AURAL_R11_OK,"render one block");
    for(unsigned j=0;j<8;j++)
        check(aural_r11_tone_render(pieces,chunked+j*256,128,256)==AURAL_R11_OK,
              "render split block");
    check(memcmp(full,chunked,sizeof full)==0,"chunk-invariant PCM");
    check(full[0]==0 && full[1]==0,"ramp zero start");
    int audible=0;
    for(unsigned i=0;i<2048;i++){
        check(abs(full[i])<=2622,"digital ceiling");
        if(i%2==1)check(full[i]==0,"right ear isolation");
        else if(full[i]!=0)audible=1;
    }
    check(audible,"nonzero left tone");
    int16_t canary[4]={111,222,333,444};
    check(aural_r11_tone_render(one,canary,2,1)==AURAL_R11_INVALID,
          "insufficient caller capacity rejected");
    check(canary[0]==111&&canary[1]==222&&canary[2]==333&&canary[3]==444,
          "invalid render does not write");
    check(aural_r11_tone_stop(one)==AURAL_R11_OK,"first stop");
    check(aural_r11_tone_stop(one)==AURAL_R11_OK,"idempotent stop");
    check(aural_r11_tone_render(one,full,1024,2048)==AURAL_R11_OK,"stopped render");
    for(unsigned i=0;i<2048;i++)check(full[i]==0,"stop silence");
    /* Consume 8-second bound and verify no resurrection. */
    for(int j=1;j<375;j++)
        check(aural_r11_tone_render(pieces,full,1024,2048)==AURAL_R11_OK,"bounded stream");
    check(aural_r11_tone_render(pieces,full,1024,2048)==AURAL_R11_OK,"after end");
    for(unsigned i=0;i<2048;i++)check(full[i]==0,"hard silence after end");
    aural_r11_tone_destroy(one);aural_r11_tone_destroy(pieces);
    c.api_version=99;
    check(aural_r11_tone_create(&c,&one)==AURAL_R11_INVALID,"unknown ABI version");
    c.api_version=1;c.seconds=120;
    check(aural_r11_tone_create(&c,&one)==AURAL_R11_INVALID,"unsafe duration");
    c.seconds=8;c.start_hz=100;
    check(aural_r11_tone_create(&c,&one)==AURAL_R11_INVALID,"low start frequency");
    aural_r11_notch_result m={sizeof m,0,0,0};
    check(aural_r11_measure_notch(10000,.5,16000,&m)==AURAL_R11_INVALID,
          "beyond Nyquist rejected");
    puts("PASS: C17 ABI bounds, memory capacity, blocks, STOP and ear isolation");
}
int main(void) {fixtures();tone();return 0;}
