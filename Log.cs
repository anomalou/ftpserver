using System;
using System.IO;
namespace FTPserver{
    static class Log{
        private static StreamWriter streamWriter;

        private static string logPath;

        private static DateTime logStart;
        private static bool isStarted;

        public static void StartLog(){
            DateTime date = DateTime.Now;
            logStart = date;
            logPath = $"{Directory.GetCurrentDirectory()}\\{date.Day}_{date.Month}_{date.Year}_{date.Hour}-{date.Minute}_logFile.ano";
            // File.Create(logPath);
            FileInfo fileInfo = new FileInfo(logPath);
            // fileInfo.Create();
            streamWriter = fileInfo.CreateText();
            streamWriter.WriteLine($"-------------Log by {CurrentDate()} begun!-------------");
            streamWriter.Flush();
            isStarted = true;
        }

        public static void Write(string message){
            if(isStarted){
                message = $"[D|{CurrentDate()}][T|{CurrentTime()}]>{message}";
                streamWriter.WriteLine(message);
                streamWriter.Flush();
            }
        }

        public static void StopLog(){
            if(isStarted){
                streamWriter.WriteLine($"^^^^^^^^^^^^^Log by {logStart.Day}.{logStart.Month}.{logStart.Year} ended in {CurrentDate()} at {CurrentTime()}!^^^^^^^^^^^^^");
                streamWriter.Flush();
                streamWriter.Close();
                isStarted = false;
            }
        }
        private static string CurrentDate(){
            DateTime date = DateTime.Today;
            return $"{date.Day}.{date.Month}.{date.Year}";
        }

        private static string CurrentTime(){
            DateTime time = DateTime.Now;
            return $"{time.Hour}:{time.Minute}:{time.Second}";
        }
    }
}