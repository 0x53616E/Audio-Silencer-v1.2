# 🎵 WPF Audio Converter & Silencer

A modern, lightweight audio processing tool built with **C# and WPF**. 
It features a sleek dark UI with glassmorphism effects, drag & drop support, and specialized audio normalization capabilities.

## ✨ Features

* **Format Conversion:**
    * `FLAC`, `MP3`, `OGG` ➔ `WAV` (PCM 16-bit)
    * `WAV` ➔ `OGG` (with special MADI naming logic)
* **Audio Engineering Tools:**
    * **EBU R128 Normalization:** Automatically analyzes and normalizes audio to **-8 LUFS**.
    * **MADI Calculation:** Calculates Start/Total MADI values based on BPM and Time Signature.
    * **Manual Offset:** Add silence or trim audio at the start (supports negative/positive values).
* **Modern UI:**
    * Dark Mode with Green Accents.
    * **Drag & Drop** support for files.
    * **Color-Coded Logging:**
        * <span style="color:lightgreen">Light Green:</span> Measurement Data (EBU, Time Sig)
        * <span style="color:lime">Lime Green:</span> Success messages
        * <span style="color:white">White:</span> Standard info
        * <span style="color:red">Red:</span> Errors
* **Under the hood:** Powered by **FFmpeg**.

## 🚀 Getting Started

### Prerequisites
* Windows OS (WPF application)
* [.NET Desktop Runtime](https://dotnet.microsoft.com/download) (Version 6.0 or higher recommended)
* **Important:** You need `ffmpeg.exe` and `ffprobe.exe`.

### Installation
1.  Clone the repository:
2.  Download **FFmpeg** builds (shared or static) from [ffmpeg.org](https://ffmpeg.org/download.html).
3.  Place `ffmpeg.exe` and `ffprobe.exe` in the same directory as the application executable (e.g., inside `bin/Debug/netX.X/`).
4.  Build and run the solution in Visual Studio.

## 📖 Usage

1.  **Select a File:** either click "SELECT & CONVERT" or simply **Drag & Drop** a music file (`.mp3`, `.wav`, `.flac`, `.ogg`) into the window.
2.  **MADI / Settings:**
    * If input is `.wav`, you will be prompted for **Offset**, **BPM**, and **Time**.
    * Uncheck *Calculate MADI Values* if you just want a simple conversion.
3.  **Normalization:** Ensure *Apply LUFS Normalization* is checked to target -8 LUFS.
4.  **Wait:** The log will show detailed EBU R128 measurements and turn green upon success.

## 🛠 Technologies

* C# / .NET
* WPF (Windows Presentation Foundation)
* FFmpeg (Audio Processing)

## 📄 License

Distributed under the [LICENSE](LICENSE.txt). See `LICENSE` for more information

---

### MIT License

Copyright (c) 2026 Sanya

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.**
