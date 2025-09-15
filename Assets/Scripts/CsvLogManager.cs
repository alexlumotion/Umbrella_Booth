using UnityEngine;
using System;
using System.IO;
using System.Text;

public class CsvLogManager : MonoBehaviour
{
    public string fileName = "logs.csv";

    private string _filePath;
    private int _nextId = 1;

    void Awake()
    {
        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        EnsureFileAndHeader();
    }

    void EnsureFileAndHeader()
    {
        string header = "Id,TimestampUtc,V1,V2,V3,V4,V5";

        if (!File.Exists(_filePath))
        {
            // створюємо новий файл із шапкою
            using (var sw = new StreamWriter(_filePath, false, new UTF8Encoding(false)))
            {
                sw.WriteLine(header);
            }
            _nextId = 1;
        }
        else
        {
            // файл існує — перевіряємо чи є шапка
            string firstLine = "";
            using (var sr = new StreamReader(_filePath))
            {
                if (!sr.EndOfStream)
                    firstLine = sr.ReadLine();
            }

            if (string.IsNullOrEmpty(firstLine) || !firstLine.Equals(header, StringComparison.OrdinalIgnoreCase))
            {
                // вставляємо шапку у початок файлу
                string allText = File.ReadAllText(_filePath);
                File.WriteAllText(_filePath, header + Environment.NewLine + allText, new UTF8Encoding(false));
            }

            // підрахунок Id — рядків мінус 1 (шапка)
            int lines = File.ReadAllLines(_filePath).Length - 1;
            _nextId = lines + 1;
        }
    }

    public void Log(string v1, string v2, string v3, string v4, string v5)
    {
        string timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string line = string.Format("{0},{1},{2},{3},{4},{5},{6}",
            _nextId, timestampUtc, v1, v2, v3, v4, v5);

        using (var sw = new StreamWriter(_filePath, true, new UTF8Encoding(false)))
        {
            sw.WriteLine(line);
        }

        _nextId++;
    }
}