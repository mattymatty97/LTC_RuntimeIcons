/*
Portion of Code from https://gist.github.com/mickdekkers/5c3c62539c057010d4497f9865060e20
By https://github.com/mickdekkers
As stated in this comment https://gist.github.com/mickdekkers/5c3c62539c057010d4497f9865060e20?permalink_comment_id=3253740#gistcomment-3253740
to be considered MIT licensed
*/

using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RuntimeIcons.Utils;

public static class TextureUtils
{
    /// <summary>
    /// Sanitizes a filename string by replacing illegal characters with underscores.
    /// </summary>
    /// <param name="dirty">The unsanitized filename string.</param>
    /// <returns>A sanitized filename string with illegal characters replaced with underscores.</returns>
    private static string SanitizeFilename (string dirty)
    {
        string invalidFileNameChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidFileNameChars);

        return Regex.Replace(dirty, invalidRegStr, "_");
    }

    /// <summary>
    /// Saves a byte array of PNG data as a PNG file.
    /// </summary>
    /// <param name="bytes">The PNG data to write to a file.</param>
    /// <param name="filename">The name of the file. This will be the current timestamp if not specified.</param>
    /// <param name="directory">The directory in which to save the file. This will be the game/Snapshots directory if not specified.</param>
    /// <returns>A FileInfo pointing to the created PNG file</returns>
    public static FileInfo SavePNG (byte[] bytes, string filename = "", string directory = "")
    {
        directory = directory != "" ? Directory.CreateDirectory(directory).FullName : Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Snapshots")).FullName;
        filename = filename != "" ? SanitizeFilename(filename) + ".png" : System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".png";
        string filepath = Path.Combine(directory, filename);

        File.WriteAllBytes(filepath, bytes);

        return new FileInfo(filepath);
    }
    
    /// <summary>
    /// Saves a Texture2D as a PNG file.
    /// </summary>
    /// <param name="tex">The Texture2D to write to a file.</param>
    /// <param name="filename">The name of the file. This will be the current timestamp if not specified.</param>
    /// <param name="directory">The directory in which to save the file. This will be the game/Snapshots directory if not specified.</param>
    /// <returns>A FileInfo pointing to the created PNG file</returns>
    public static FileInfo SavePNG (this Texture2D tex, string filename = "", string directory = "")
    {
        return SavePNG(tex.EncodeToPNG(), filename, directory);
    }
    
    
    public static bool IsTransparent(this Texture2D tex)
    {
        for (var x = 0; x < tex.width; x++)
        for (var y = 0; y < tex.height; y++)
            if (tex.GetPixel(x, y).a != 0)
                return false;
        return true;
    }
}