using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RSDSwissKnife
{
    internal class NameCasingFixer
    {
        private static readonly HashSet<string> TypesSet = new(StringComparer.OrdinalIgnoreCase)
        {
            "Convinit", "Noboxconv", "HitByC", "HitByW", "NHitByC", "Idlereply", "AIDestroy", "Air", "Arrive", "Bcrash",
            "Break", "Burn", "Carway", "Damage", "Dcar", "Door", "HitP", "Mcrash", "MissP", "Time",
            "Activate", "Askfood", "Askride", "Breakca", "Card", "Char", "Fall", "GIC", "GOC", "Greeting",
            "Mfail", "Mstart", "Mvic", "NewAI", "ObjectW", "Pass", "Passed", "Ridereply", "Springboard", "Tail",
            "Turbo", "Answer", "CarWay", "BreakCa", "Foodreply", "LongJump", "MissA", "Doorbell", "BreakCA", "Idle",
            "HitCar", "Gil", "CarBuy", "Destroy", "Dodge"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".rsd", ".wav", ".ogg"
        };

        public static void FixNameCase(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            string[] files = Directory.GetFiles(folderPath);
            List<string> targetFiles = new();
            List<string> invalidFiles = new();

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                string extension = Path.GetExtension(filePath);

                bool containsKeyword = TypesSet.Any(keyword =>
                    fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (!containsKeyword)
                    continue;

                if (!AllowedExtensions.Contains(extension))
                {
                    invalidFiles.Add(fileName);
                }
                else
                {
                    targetFiles.Add(filePath);
                }
            }

            if (invalidFiles.Count > 0)
            {
                string errorList = string.Join("\n", invalidFiles.Take(5));
                string extraCount = invalidFiles.Count > 5 ? $"\n...and {invalidFiles.Count - 5} others." : "";

                MessageBox.Show(
                    $"Found dialogue keywords in unsupported file formats!\n\nOnly .RSD, .WAV, and .OGG are allowed.\n\nSkipped files:\n{errorList}{extraCount}",
                    "Invalid Audio File Format",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (targetFiles.Count == 0)
            {
                MessageBox.Show("No suspected dialogue lines detected in this folder.", "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<string> filesNeedingFix = new();
            foreach (string filePath in targetFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string formattedName = fileName;

                foreach (string keyword in TypesSet)
                {
                    formattedName = ReplaceCaseInsensitive(formattedName, keyword, keyword);
                }

                string[] segments = formattedName.Split('_');
                for (int k = 0; k < segments.Length; k++)
                {
                    if (k != 1 && segments[k].Length > 0)
                    {
                        segments[k] = char.ToUpper(segments[k][0]) + segments[k].Substring(1);
                    }
                }
                formattedName = string.Join("_", segments);

                // Target L<num> followed by any letter (M, R, B, etc.) and uppercase it
                formattedName = Regex.Replace(formattedName, @"(L\d+)([a-z])", m => m.Groups[1].Value + m.Groups[2].Value.ToUpper(), RegexOptions.IgnoreCase);

                if (!string.Equals(fileName, formattedName, StringComparison.Ordinal))
                {
                    filesNeedingFix.Add(filePath);
                }
            }

            if (filesNeedingFix.Count == 0)
            {
                MessageBox.Show("All dialogue lines are named correctly.", "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"{filesNeedingFix.Count} suspected incorrect dialogue file names detected, would you like to fix them?\n\nThis is necessary for custom dialogue, as the file names require a specific type of uppercase formatting to be registered by the game.\n\nTHIS WILL RENAME ALL DETECTED INCORRECT DIALOGUE FILE NAMES!",
                "Fix Name Cases",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            int renamedCount = 0;

            foreach (string filePath in filesNeedingFix)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string newFileName = fileName;

                    foreach (string keyword in TypesSet)
                    {
                        newFileName = ReplaceCaseInsensitive(newFileName, keyword, keyword);
                    }

                    string[] segments = newFileName.Split('_');
                    for (int k = 0; k < segments.Length; k++)
                    {
                        if (k != 1 && segments[k].Length > 0)
                        {
                            segments[k] = char.ToUpper(segments[k][0]) + segments[k].Substring(1);
                        }
                    }
                    newFileName = string.Join("_", segments);

                    // Target L<num> followed by any letter (M, R, B, etc.) and uppercase it
                    newFileName = Regex.Replace(newFileName, @"(L\d+)([a-z])", m => m.Groups[1].Value + m.Groups[2].Value.ToUpper(), RegexOptions.IgnoreCase);

                    if (!string.Equals(fileName, newFileName, StringComparison.Ordinal))
                    {
                        string targetPath = Path.Combine(folderPath, newFileName);
                        string tempPath = Path.Combine(folderPath, Guid.NewGuid().ToString() + ".tmp");

                        File.Move(filePath, tempPath);
                        File.Move(tempPath, targetPath);

                        renamedCount++;
                    }
                }
                catch
                {

                }
            }

            MessageBox.Show($"Done! Successfully updated cases for {renamedCount} file(s).\n\nPlease refresh the list.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string ReplaceCaseInsensitive(string input, string search, string replacement)
        {
            return Regex.Replace(
                input,
                Regex.Escape(search),
                replacement,
                RegexOptions.IgnoreCase);
        }
    }
}