//UNITY_SHADER_NO_UPGRADE
#ifndef SHF_SP_GEMANIMATION_00_INCLUDED
#define SHF_SP_GEMANIMATION_00_INCLUDED

void GetYOffset_float(float time, float positionX, float frequency, float amplitude, out float Out)
{
    Out = sin((time + floor(positionX) * 12 )  * frequency) * amplitude;
}

void GetCulledVertex_float(float positionX, float fillAmmount, float3 position, out float3 Out)
{
    float NaN = asfloat(0x7fc00000);
    Out = floor(positionX + 1) > fillAmmount * 13 ? float3(NaN,NaN,NaN) :  position;
}
#endif //SHF_SP_GEMANIMATION_00_INCLUDED