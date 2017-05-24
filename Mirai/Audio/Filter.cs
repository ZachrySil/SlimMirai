﻿
namespace Mirai.Audio
{
    class Filter
    {
        internal static double Volume = 1;
        internal static double Tone = 1;

        internal static string Tag
        {
            get
            {
                return $"volume={Volume},asetrate=r={44.1 * Tone}K,atempo={1 / Tone},afade=t=in:d=0.4:curve=squ";
            }
        }
    }
}