using CatfortSound.SoundEngine.Banks;
using CatfortSound.SoundEngine.Channels;
using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.DataTables
{
    public enum ChannelIndexes
    {
        SQUARE_1 = 0,
        SQUARE_2 = 1,
        TRIANGLE = 2,
        NOISE = 3,
        DPMC = 4,
        //FDS = 5,
    }
    public enum Streams
    {
        MUSIC_SQ1,
        MUSIC_SQ2,
        MUSIC_TRI,
        MUSIC_NOI,
        MUSIC_DPMC
    }

    public enum Lengths
    {
        _ = 0x00,   //continue with no change
        l32 = 0x80,
        l16 = 0x81,
        l8 = 0x82,
        l4 = 0x83,
        l2 = 0x84,
        l1 = 0x85,
        d16 = 0x86,
        d8 = 0x87,
        d4 = 0x88,
        d2 = 0x89,
        d1 = 0x8A,
        t16 = 0x8B,
        t8 = 0x8C,
        t4 = 0x8D,
    }

    public static class NoteConstants
    {
        public static int Rest = 0x5e;

        public static float GetTicks(byte idx) => LenTable[idx - 0x80];
        public static readonly float[] LenTable =
        {
            1, 2, 4, 8, 16, 32, 1 + 2, 2 + 4, 4 + 8, 8 + 16, 16 + 32, 4f/3f, 8f/3f, 16f/3f
        };
    }

    public enum Notes
    {
        rest = -1,
        A = 0x00,
        As_Bb = 0x01,
        B = 0x02,
        C = 0x03,
        CsDb = 0x04,
        D = 0x05,
        DsEb = 0x06,
        E = 0x07,
        F = 0x08,
        FsGb = 0x09,
        G = 0x0A,
        GsAb = 0x0B,
    }

    public static class NoteTables
    {
        public static readonly uint[] NoteTable =
        {
            // A    As/Bf    B      C    Cs/Df    D    Ds/Ef    E      F    Fs/Gf    G    Gs/Af
            0x7F1, 0x780, 0x713, 0x6AD, 0x64D, 0x5F3, 0x59D, 0x54D, 0x500, 0x4B8, 0x475, 0x435,
            0x3F8, 0x3BF, 0x389, 0x356, 0x326, 0x2F9, 0x2CE, 0x2A6, 0x27F, 0x25C, 0x23A, 0x21A,
            0x1FB, 0x1DF, 0x1C4, 0x1AB, 0x193, 0x17C, 0x167, 0x152, 0x13F, 0x12D, 0x11C, 0x10C,
            0x0FD, 0x0EF, 0x0E2, 0x0D2, 0x0C9, 0x0BD, 0x0B3, 0x0A9, 0x09F, 0x096, 0x08E, 0x086,
            0x07E, 0x077, 0x070, 0x06A, 0x064, 0x05E, 0x059, 0x054, 0x04F, 0x04B, 0x046, 0x042,
            0x03F, 0x03B, 0x038, 0x034, 0x031, 0x02F, 0x02C, 0x029, 0x027, 0x025, 0x023, 0x021,
            0x01F, 0x01D, 0x01B, 0x01A, 0x018, 0x017, 0x015, 0x014, 0x013, 0x012, 0x011, 0x010,
            0x00F, 0x00E, 0x00D, 0x00C, 0x00C, 0x00B, 0x00A, 0x00A, 0x009, 0x008, 0x000
        };
    }

    public struct DMCNote
    {
        //pitch
        public DMCSamples Sample;
        public int DMCPitch;
        public int TickLengh;

        public DMCNote(DMCSamples sample, int pitch, int tickLength)
        {
            Sample = sample;
            DMCPitch = pitch;
            TickLengh = tickLength;
        }
    }

}
