#include "survivalsim_native.h"
#include <stdint.h>

int32_t calculate_gathering_cost(
    int32_t base_cost,
    int32_t tool_efficiency_percent,
    int32_t difficulty_percent)
{
    if (base_cost <= 0 ||
        tool_efficiency_percent <= 0 ||
        difficulty_percent <= 0)
    {
        return -1;
    }

    /* 64-bit arithmetic prevents intermediate 32-bit overflow. */
    int64_t numerator = (int64_t)base_cost * (int64_t)difficulty_percent;
    int64_t result =
        (numerator + tool_efficiency_percent - 1) / tool_efficiency_percent;

    if (result <= 0 || result > INT32_MAX)
        return -2;

    return (int32_t)result;
}
