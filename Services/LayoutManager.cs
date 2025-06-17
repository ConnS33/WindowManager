using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using WindowManager.Models;

namespace WindowManager.Services
{
    public class LayoutManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowManager");
            
        private static readonly string LayoutsPath = Path.Combine(AppDataPath, "layouts");
        
        public LayoutManager()
        {
            // Ensure directories exist
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(LayoutsPath);
        }
        
        public void SaveLayout(SavedLayout layout)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(layout, options);
            string filePath = Path.Combine(LayoutsPath, $"{layout.Name}.json");
            File.WriteAllText(filePath, json);
        }
        
        public List<SavedLayout> LoadAllLayouts()
        {
            var layouts = new List<SavedLayout>();
            
            if (!Directory.Exists(LayoutsPath))
                return layouts;
                
            foreach (var file in Directory.GetFiles(LayoutsPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var layout = JsonSerializer.Deserialize<SavedLayout>(json);
                    if (layout != null)
                        layouts.Add(layout);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading layout {file}: {ex.Message}");
                }
            }
            
            return layouts;
        }
        
        public void DeleteLayout(string layoutName)
        {
            string filePath = Path.Combine(LayoutsPath, $"{layoutName}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
