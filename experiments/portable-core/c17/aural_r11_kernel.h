/* Experimental R11 portable C17 ABI. NOT Aural production DSP or physical playback. */
#ifndef AURAL_R11_KERNEL_H
#define AURAL_R11_KERNEL_H
#include <stdint.h>
#ifdef __cplusplus
extern "C" {
#endif

#define AURAL_R11_API_VERSION UINT32_C(1)
#define AURAL_R11_RATE UINT32_C(48000)
#define AURAL_R11_CEILING 0.08
#define AURAL_R11_MAX_FRAMES UINT32_C(4096)

enum aural_r11_status {
    AURAL_R11_OK = 0,
    AURAL_R11_INVALID = 1,
    AURAL_R11_NOMEM = 2
};
enum aural_r11_ear {
    AURAL_R11_BOTH = 0, AURAL_R11_LEFT = 1, AURAL_R11_RIGHT = 2
};
typedef struct aural_r11_tone aural_r11_tone;
typedef struct {
    uint32_t struct_size;
    uint32_t api_version;
    double start_hz;
    double end_hz;
    uint32_t seconds;
    uint32_t ear;
} aural_r11_tone_config;
typedef struct {
    uint32_t struct_size;
    double lower_hz;
    double upper_hz;
    double measured_width_octaves;
} aural_r11_notch_result;

uint32_t aural_r11_api_version(void);
int aural_r11_tone_create(const aural_r11_tone_config *config, aural_r11_tone **out);
int aural_r11_tone_frequency(const aural_r11_tone_config *config, uint64_t frame, double *out_hz);
/* Caller owns memory. buffer_capacity_samples >= frames*2; no allocation in render. */
int aural_r11_tone_render(aural_r11_tone *handle, int16_t *out_samples,
                         uint32_t frames, uint32_t buffer_capacity_samples);
int aural_r11_tone_stop(aural_r11_tone *handle);
void aural_r11_tone_destroy(aural_r11_tone *handle);
int aural_r11_measure_notch(double center_hz, double width_octaves,
                           uint32_t sample_rate_hz, aural_r11_notch_result *out);
#ifdef __cplusplus
}
#endif
#endif
