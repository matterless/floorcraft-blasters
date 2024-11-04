#ifndef MATTERLESS_COMMON_INCLUDED
#define MATTERLESS_COMMON_INCLUDED

inline half4 SummonEffect(half4 color, float2 uv, float time, float maxRange, float gradient, sampler _NoiseTexture, float factor)
{
    half noise = tex2D(_NoiseTexture, uv * 5 - float2(0, time * 0.4)).r * 0.5  *
        tex2D(_NoiseTexture, uv * 4 - float2(0, time * 0.4)).g * 2.0 - 1.0;
    
    noise += 1 - gradient + factor * maxRange * 1.2 - 1;
    color.rgb += saturate(1 - noise) * 5;
    color.a = saturate(noise);
    
    return color;
}

#ifdef FLIP_BOOK_ANIMATION
#define FRAME_RATE 12.0f

inline float2 FlipBookUV(float2 frameSize, float startingFrame, float2 uv)
{
    const float offset = fmod(floor(startingFrame), NUMBER_OF_FRAMES);
    const float xOffset = fmod(offset, NUMBER_OF_ROWS) * frameSize.x;
    const float yOffset = floor(offset / NUMBER_OF_COLUMNS) * frameSize.y;
    const float x = fmod(floor(_Time.y * FRAME_RATE), NUMBER_OF_ROWS) * frameSize.x + xOffset;
    const float y = floor(_Time.y * FRAME_RATE/ NUMBER_OF_COLUMNS) * frameSize.y + yOffset;
    return float2(x, -y) + uv;
}
#endif

#endif
