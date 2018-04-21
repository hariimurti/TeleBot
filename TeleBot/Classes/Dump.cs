﻿using System;
using System.IO;

namespace TeleBot.Classes
{
    public static class Dump
    {
        private static Log _log = new Log("Dump");
        
        public static void ToFile(string fileName, string text)
        {
            try
            {
                var filePath = Program.FilePathInData(fileName);
                
                _log.Debug("Simpan {0}", filePath);
                
                File.WriteAllText(filePath, text);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }
    }
}