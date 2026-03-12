using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{

    public enum ChannelIndexes
    {
        SQUARE_1 = 0,
        SQUARE_2 = 1,
        TRIANGLE = 2,
        NOISE = 3,
        DMC = 4,
        FDS = 5,
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

    public static class NoteClass
    {
        public static readonly byte A1 = 0x00;
        public static readonly byte As1 = 0x01;
        public static readonly byte Bb1 = 0x01;
        public static readonly byte B1 = 0x02;

        public static readonly byte C2 = 0x03;
        public static readonly byte Cs2 = 0x04;
        public static readonly byte Db2 = 0x04;
        public static readonly byte D2 = 0x05;
        public static readonly byte Ds2 = 0x06;
        public static readonly byte Eb2 = 0x06;
        public static readonly byte E2 = 0x07;
        public static readonly byte F2 = 0x08;
        public static readonly byte Fs2 = 0x09;
        public static readonly byte Gb2 = 0x09;
        public static readonly byte G2 = 0x0A;
        public static readonly byte Gs2 = 0x0B;
        public static readonly byte Ab2 = 0x0B;
        public static readonly byte A2 = 0x0C;
        public static readonly byte As2 = 0x0D;
        public static readonly byte Bb2 = 0x0D;
        public static readonly byte B2 = 0x0E;

        public static readonly byte C3 = 0x0F;
        public static readonly byte Cs3 = 0x10;
        public static readonly byte Db3 = 0x10;
        public static readonly byte D3 = 0x11;
        public static readonly byte Ds3 = 0x12;
        public static readonly byte Eb3 = 0x12;
        public static readonly byte E3 = 0x13;
        public static readonly byte F3 = 0x14;
        public static readonly byte Fs3 = 0x15;
        public static readonly byte Gb3 = 0x15;
        public static readonly byte G3 = 0x16;
        public static readonly byte Gs3 = 0x17;
        public static readonly byte Ab3 = 0x17;
        public static readonly byte A3 = 0x18;
        public static readonly byte As3 = 0x19;
        public static readonly byte Bb3 = 0x19;
        public static readonly byte B3 = 0x1a;

        public static readonly byte C4 = 0x1b;
        public static readonly byte Cs4 = 0x1c;
        public static readonly byte Db4 = 0x1c;
        public static readonly byte D4 = 0x1d;
        public static readonly byte Ds4 = 0x1e;
        public static readonly byte Eb4 = 0x1e;
        public static readonly byte E4 = 0x1f;
        public static readonly byte F4 = 0x20;
        public static readonly byte Fs4 = 0x21;
        public static readonly byte Gb4 = 0x21;
        public static readonly byte G4 = 0x22;
        public static readonly byte Gs4 = 0x23;
        public static readonly byte Ab4 = 0x23;
        public static readonly byte A4 = 0x24;
        public static readonly byte As4 = 0x25;
        public static readonly byte Bb4 = 0x25;
        public static readonly byte B4 = 0x26;

        public static readonly byte C5 = 0x27;
        public static readonly byte Cs5 = 0x28;
        public static readonly byte Db5 = 0x28;
        public static readonly byte D5 = 0x29;
        public static readonly byte Ds5 = 0x2a;
        public static readonly byte Eb5 = 0x2a;
        public static readonly byte E5 = 0x2b;
        public static readonly byte F5 = 0x2c;
        public static readonly byte Fs5 = 0x2d;
        public static readonly byte Gb5 = 0x2d;
        public static readonly byte G5 = 0x2e;
        public static readonly byte Gs5 = 0x2f;
        public static readonly byte Ab5 = 0x2f;
        public static readonly byte A5 = 0x30;
        public static readonly byte As5 = 0x31;
        public static readonly byte Bb5 = 0x31;
        public static readonly byte B5 = 0x32;

        public static readonly byte C6 = 0x33;
        public static readonly byte Cs6 = 0x34;
        public static readonly byte Db6 = 0x34;
        public static readonly byte D6 = 0x35;
        public static readonly byte Ds6 = 0x36;
        public static readonly byte Eb6 = 0x36;
        public static readonly byte E6 = 0x37;
        public static readonly byte F6 = 0x38;
        public static readonly byte Fs6 = 0x39;
        public static readonly byte Gb6 = 0x39;
        public static readonly byte G6 = 0x3a;
        public static readonly byte Gs6 = 0x3b;
        public static readonly byte Ab6 = 0x3b;
        public static readonly byte A6 = 0x3c;
        public static readonly byte As6 = 0x3d;
        public static readonly byte Bb6 = 0x3d;
        public static readonly byte B6 = 0x3e;

        public static readonly byte C7 = 0x3f;
        public static readonly byte Cs7 = 0x40;
        public static readonly byte Db7 = 0x40;
        public static readonly byte D7 = 0x41;
        public static readonly byte Ds7 = 0x42;
        public static readonly byte Eb7 = 0x42;
        public static readonly byte E7 = 0x43;
        public static readonly byte F7 = 0x44;
        public static readonly byte Fs7 = 0x45;
        public static readonly byte Gb7 = 0x45;
        public static readonly byte G7 = 0x46;
        public static readonly byte Gs7 = 0x47;
        public static readonly byte Ab7 = 0x47;
        public static readonly byte A7 = 0x48;
        public static readonly byte As7 = 0x49;
        public static readonly byte Bb7 = 0x49;
        public static readonly byte B7 = 0x4a;

        public static readonly byte C8 = 0x4b;
        public static readonly byte Cs8 = 0x4c;
        public static readonly byte Db8 = 0x4c;
        public static readonly byte D8 = 0x4d;
        public static readonly byte Ds8 = 0x4e;
        public static readonly byte Eb8 = 0x4e;
        public static readonly byte E8 = 0x4f;
        public static readonly byte F8 = 0x50;
        public static readonly byte Fs8 = 0x51;
        public static readonly byte Gb8 = 0x51;
        public static readonly byte G8 = 0x52;
        public static readonly byte Gs8 = 0x53;
        public static readonly byte Ab8 = 0x53;
        public static readonly byte A8 = 0x54;
        public static readonly byte As8 = 0x55;
        public static readonly byte Bb8 = 0x55;
        public static readonly byte B8 = 0x56;

        public static readonly byte C9 = 0x57;
        public static readonly byte Cs9 = 0x58;
        public static readonly byte Db9 = 0x58;
        public static readonly byte D9 = 0x59;
        public static readonly byte Ds9 = 0x5a;
        public static readonly byte Eb9 = 0x5a;
        public static readonly byte E9 = 0x5b;
        public static readonly byte F9 = 0x5c;
        public static readonly byte Fs9 = 0x5d;
        public static readonly byte Gb9 = 0x5d;

        public static readonly byte rest = 0x5e;

    }

    public static class NoteTables
    {

        public static readonly uint[] NoteTable =
        {
            // 
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
        public DMC.Samples Sample;
        public int DMCPitch;
        public int TickLengh;

        public DMCNote(DMC.Samples sample, int pitch, int tickLength)
        {
            Sample = sample;
            DMCPitch = pitch;
            TickLengh = tickLength;
        }
    }

}
