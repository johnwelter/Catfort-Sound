using CatfortSound.SoundEngine;
using CatfortSound.SoundEngine.SongData;
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
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

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
        private static readonly Action EmptyDelegate = delegate { };
        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }

}
