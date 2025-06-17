using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            
            Debug.WriteLine($"Looking for layouts in: {LayoutsPath}");
            
            if (!Directory.Exists(LayoutsPath))
            {
                Debug.WriteLine("Layouts directory does not exist");
                return layouts;
            }
                
            var files = Directory.GetFiles(LayoutsPath, "*.json");
            Debug.WriteLine($"Found {files.Length} layout files");
                
            foreach (var file in files)
            {
                try
                {
                    Debug.WriteLine($"Loading layout from: {file}");
                    string json = File.ReadAllText(file);
                    var layout = JsonSerializer.Deserialize<SavedLayout>(json);
                    if (layout != null)
                    {
                        Debug.WriteLine($"Successfully loaded layout: {layout.Name} with {layout.Zones?.Count ?? 0} zones");
                        layouts.Add(layout);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to deserialize layout from {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading layout {file}: {ex}");
                }
            }
            
            Debug.WriteLine($"Returning {layouts.Count} loaded layouts");
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
