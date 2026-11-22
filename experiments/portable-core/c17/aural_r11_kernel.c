/* R11 C17 candidate: deterministic reference-tone / notch-response probe ONLY. */
#include "aural_r11_kernel.h"
#include <math.h>
#include <stdint.h>
#include <stdlib.h>
#include <limits.h>

#define AURAL_PI 3.141592653589793238462643383279502884

struct aural_r11_tone {
    aural_r11_tone_config config;
    uint64_t frame, total;
    double phase, log_step;
    int stopped;
};
static int valid(const aural_r11_tone_config *p) {
    return p && p->struct_size == sizeof(*p) && p->api_version == AURAL_R11_API_VERSION &&
        isfinite(p->start_hz) && isfinite(p->end_hz) &&
        p->start_hz >= 200.0 && p->start_hz <= 10000.0 &&
        p->end_hz >= 200.0 && p->end_hz <= 10000.0 &&
        p->start_hz != p->end_hz && p->seconds >= 8 && p->seconds <= 15 &&
        p->ear <= AURAL_R11_RIGHT;
}
uint32_t aural_r11_api_version(void) { return AURAL_R11_API_VERSION; }
int aural_r11_tone_frequency(const aural_r11_tone_config *cfg, uint64_t frame, double *out_hz) {
    if (!valid(cfg) || !out_hz || frame >= (uint64_t)cfg->seconds * AURAL_R11_RATE)
        return AURAL_R11_INVALID;
    uint64_t total = (uint64_t)cfg->seconds * AURAL_R11_RATE;
    *out_hz = cfg->start_hz * exp(log(cfg->end_hz/cfg->start_hz) *
                                  ((double)frame / (double)(total - UINT64_C(1))));
    return AURAL_R11_OK;
}
int aural_r11_tone_create(const aural_r11_tone_config *cfg, aural_r11_tone **out) {
    if (out) *out = NULL;
    if (!valid(cfg) || !out) return AURAL_R11_INVALID;
    aural_r11_tone *p = (aural_r11_tone *)calloc(1,sizeof(*p));
    if (!p) return AURAL_R11_NOMEM;
    p->config = *cfg;
    p->total = (uint64_t)cfg->seconds * AURAL_R11_RATE;
    p->log_step = log(cfg->end_hz / cfg->start_hz) / (double)(p->total-UINT64_C(1));
    *out = p;
    return AURAL_R11_OK;
}
int aural_r11_tone_render(aural_r11_tone *p, int16_t *samples,
                         uint32_t frames, uint32_t capacity) {
    if (!p || !samples || frames == 0 || frames > AURAL_R11_MAX_FRAMES ||
        capacity < frames * UINT32_C(2)) return AURAL_R11_INVALID;
    /* No allocations, I/O, locks, syscalls, logging or callbacks in the audio rendering loop. */
    for (uint32_t i=0; i<frames; ++i) {
        int16_t v = 0;
        if (!p->stopped && p->frame < p->total) {
            double hz = p->config.start_hz * exp(p->log_step * (double)p->frame);
            p->phase += (2.0 * AURAL_PI * hz / (double)AURAL_R11_RATE);
            if (p->phase >= 2.0*AURAL_PI) p->phase -= 2.0*AURAL_PI;
            double ramp = fmin(1.0, (double)p->frame / (0.1*AURAL_R11_RATE));
            double fade = fmin(1.0, (double)(p->total-p->frame) / (0.1*AURAL_R11_RATE));
            double scaled = sin(p->phase) * AURAL_R11_CEILING * (double)INT16_MAX * ramp * fade;
            long rounded = lrint(scaled);
            if (rounded > INT16_MAX) rounded = INT16_MAX;
            if (rounded < INT16_MIN) rounded = INT16_MIN;
            v = (int16_t)rounded;
        }
        samples[2*i] = (p->config.ear == AURAL_R11_RIGHT) ? 0 : v;
        samples[2*i+1] = (p->config.ear == AURAL_R11_LEFT) ? 0 : v;
        if (p->frame < p->total) ++p->frame;
    }
    return AURAL_R11_OK;
}
int aural_r11_tone_stop(aural_r11_tone *p) {
    if (!p) return AURAL_R11_INVALID;
    p->stopped = 1;
    return AURAL_R11_OK;
}
void aural_r11_tone_destroy(aural_r11_tone *p) { free(p); }

typedef struct { double b1, a0, a2; uint32_t sr; } response;
static double magnitude(const response *r, double hz) {
    double w = 2.0*AURAL_PI*hz/r->sr;
    double re = 1.0+r->b1*cos(w)+cos(2.0*w);
    double im = -r->b1*sin(w)-sin(2.0*w);
    double dre = r->a0+r->b1*cos(w)+r->a2*cos(2.0*w);
    double dim = -r->b1*sin(w)-r->a2*sin(2.0*w);
    return sqrt((re*re+im*im)/(dre*dre+dim*dim));
}
int aural_r11_measure_notch(double center, double width,
                           uint32_t sr, aural_r11_notch_result *out) {
    if (!out || out->struct_size != sizeof(*out) ||
        sr < 8000 || sr > 384000 || !isfinite(center) ||
        center <= 0 || center >= sr/2.0 || !isfinite(width) ||
        width < 0.1 || width > 2.0) return AURAL_R11_INVALID;
    double r=pow(2.0,width);
    double q=sqrt(r)/(r-1.0);
    double w0=2.0*AURAL_PI*center/sr;
    double alpha=sin(w0)/(2.0*q);
    response p = {-2.0*cos(w0), 1.0+alpha, 1.0-alpha, sr};
    const double threshold=0.7071067811865475244;
    double l=0,h=center;
    for(int i=0;i<64;i++) {
        double mid=(l+h)/2.0;
        if(magnitude(&p,mid)>threshold)l=mid;else h=mid;
    }
    double lower=(l+h)/2.0;
    l=center;h=sr/2.0;
    for(int i=0;i<64;i++) {
        double mid=(l+h)/2.0;
        if(magnitude(&p,mid)<threshold)l=mid;else h=mid;
    }
    double upper=(l+h)/2.0;
    out->lower_hz=lower;
    out->upper_hz=upper;
    out->measured_width_octaves=log2(upper/lower);
    return AURAL_R11_OK;
}
