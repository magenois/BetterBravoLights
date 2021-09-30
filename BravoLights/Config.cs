﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace BravoLights
{
    class Config
    {
        /// <summary>
        /// A cooloff timer to prevent us reading the config file as soon as it changes.
        /// </summary>
        private readonly Timer backoffTimer = new Timer { AutoReset = false, Interval = 100 };

        private FileSystemWatcher fsWatcher;

        public Config()
        {
            backoffTimer.Elapsed += BackoffTimer_Elapsed;
        }


        public event EventHandler OnConfigChanged;

        public void Monitor()
        {
            fsWatcher = new FileSystemWatcher(System.Windows.Forms.Application.StartupPath);
            fsWatcher.Changed += ConfigFileChanged;
            fsWatcher.Created += ConfigFileChanged;
            fsWatcher.EnableRaisingEvents = true;

            ReadConfig();
        }

        private void BackoffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReadConfig();
        }

        private void ConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains("Config.ini", StringComparison.InvariantCultureIgnoreCase))
            {
                backoffTimer.Stop();
                backoffTimer.Start();
            }
        }


        private static readonly Regex sectionRegex = new Regex("\\[(.*)\\]");
        private static readonly Regex keyValueRegex = new Regex("^(.*?)\\s*=\\s*(.*)$");

        public string GetConfig(string aircraft, string key)
        {
            IniSection section;
            if (sections.TryGetValue($"Aircraft.{aircraft}", out section))
            {
                string value;
                if (section.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            if (sections.TryGetValue("Default", out section))
            {
                string value;
                if (section.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            return null;
        }

        private Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();

        private void ReadConfig()
        {
            Debug.WriteLine("Reading config file");

            string[] configLines;

            try
            {
                configLines = File.ReadAllLines(Path.Join(System.Windows.Forms.Application.StartupPath, "Config.ini"));
            } catch
            {
                Debug.WriteLine("Failed to read file");
                return;
            }

            IniSection currentSection = null;

            var sections = new Dictionary<string, IniSection>();

            foreach (var rawLine in configLines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith(";"))
                {
                    // Comment
                    continue;
                }

                if (line.Length == 0) {
                    // Empty line
                    continue;
                }

                var sectionMatch = sectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    var sectionName = sectionMatch.Groups[1].Value;
                    currentSection = new IniSection(sectionName);
                    sections.Add(sectionName, currentSection);
                    continue;
                }


                var keyValueMatch = keyValueRegex.Match(line);
                if (keyValueMatch.Success)
                {
                    var key = keyValueMatch.Groups[1].Value;
                    var value = keyValueMatch.Groups[2].Value;
                    currentSection.Add(key, value);
                }
            }

            this.sections = sections;

            if (this.OnConfigChanged != null)
            {
                this.OnConfigChanged(this, EventArgs.Empty);
            }
        }
    }

    class IniSection
    {
        private readonly string SectionName;
        private readonly Dictionary<string, string> storage = new Dictionary<string, string>();

        public IniSection(string sectionName)
        {
            SectionName = sectionName;
        }

        public void Add(string key, string value)
        {
            storage[key] = value;
        }

        public bool TryGetValue(string key, out string value)
        {
            return storage.TryGetValue(key, out value);
        }
    }
}
