#ifndef SURVIVALSIM_NATIVE_H
#define SURVIVALSIM_NATIVE_H

#include <stdint.h>

#ifdef _WIN32
#define SURVIVALSIM_API __declspec(dllexport)
#else
#define SURVIVALSIM_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Uses fixed-width 32-bit values to match C# System.Int32.
   Returns ceil(base_cost * difficulty_percent / tool_efficiency_percent).
   Returns -1 for invalid input and -2 when the result overflows int32_t. */
SURVIVALSIM_API int32_t calculate_gathering_cost(
    int32_t base_cost,
    int32_t tool_efficiency_percent,
    int32_t difficulty_percent);

#ifdef __cplusplus
}
#endif
#endif
