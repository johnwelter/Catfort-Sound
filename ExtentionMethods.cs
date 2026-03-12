using CatfortSound.SoundEngine;
using CatfortSound.ViewModels;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace CatfortSound
{
    // Source - https://stackoverflow.com/a/1213649
    // Posted by Neil, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-03-11, License - CC BY-SA 2.5
    public static class ExtensionMethods
    {
        // Deep clone
        public static T DeepClone<T>(this T a)
        {
            var json = JsonSerializer.Serialize(a);
            return JsonSerializer.Deserialize<T>(json);
        }

        public static void ReadEntryBytes<T>(this T entryList, byte[] bytes, int width)
        {
            System.Collections.IList convertedList = entryList as System.Collections.IList;            
            for(int i = 0; i < bytes.Length; i += width)
            {
                byte[] shortArray = new byte[width];
                Array.Copy(bytes, i, shortArray, 0, width);
                var itemType = convertedList.GetType().GetGenericArguments().Single();
                ConstructorInfo ctor = itemType.GetConstructor(new Type[] { typeof(byte[]) });
                var instance = ctor.Invoke(new object[] {shortArray});
                convertedList.Add(instance);
            }
        }

        public static void ClearChannel<T>(this T entryList)
        {
            System.Collections.IList convertedList = entryList as System.Collections.IList;
            convertedList.Clear();
        }
        public static byte[] GetEntryBytes<T>(this T entryList, int idx = -1) 
        {
            System.Collections.IList convertedList = entryList as System.Collections.IList;

            if(convertedList.Count == 0)
            {
                return new byte[0];
            }

            if(idx != -1)
            {
                return convertedList[idx].GetReflEntryBytes();
            }

            List<byte> cumlulativeBytes = new List<byte>();
            foreach(var item in convertedList)
            {
                cumlulativeBytes.AddRange(item.GetReflEntryBytes(true));
            }
            return cumlulativeBytes.ToArray();
        }

        public static byte[] GetReflEntryBytes<T>(this T entry, bool keepEmpty = false)
        {
            MethodInfo getEntry = entry.GetType().GetMethod("GetEntryBytes");
            return (byte[])getEntry.Invoke(entry, new object[] {keepEmpty});
        }
        public static void ReadReflEntryBytes<T>(this T entry,  byte[] bytes)
        {

        }
    }

}
