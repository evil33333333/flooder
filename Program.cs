﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Flooder
{
    internal class Program
    {   
        public static int attempts;
        public static List<Thread> threads = new List<Thread>();

        public struct WorkerData
        {
            public String cookie;
            public String thread_id;
            public String text;
            public int thread_count;
            public int sleep_time;
        }

        static void Main()
        {
            WorkerData workerData = new WorkerData
            {
                text = System.IO.File.ReadAllText("message.txt")
            };

            while (String.IsNullOrEmpty(workerData.cookie))
            {
                Program.Log("[!] Status: Not logged in.", ConsoleColor.DarkRed, true);

                Console.Clear();
                Console.Write("[+] Username: "); 
                String username = Console.ReadLine();
                Console.Write("[+] Password: ");
                String password = Console.ReadLine();
                workerData.cookie = Login(username, password);
            }

            Program.Log("[!] Status: Successfully logged in.", ConsoleColor.Green, true);

            Thread.Sleep(TimeSpan.FromSeconds(2));
            Console.Write("[+] Thread ID: ");
            workerData.thread_id = Console.ReadLine();
            Console.Write("[+] Thread Amount: ");
            workerData.thread_count = Convert.ToInt32(Console.ReadLine());
            Console.Write("[+] Sleep Time: ");
            workerData.sleep_time = Convert.ToInt32(Console.ReadLine());

            Program.Log("[!] Starting worker threads...", ConsoleColor.DarkBlue, true);

            Program.StartWorkers(workerData);
            Task.Factory.StartNew(() =>
            {
                foreach (Thread _t in threads) 
                { 
                    _t.Join(); 
                }
            }); 
            new Thread(() => 
            { 
                while (true) 
                { 
                    Console.Write($"[!] Sent {attempts} messages to thread_id \"{workerData.thread_id}\"!\r"); 
                } 
            }).Start();
            Console.ReadKey();

        }

        public static String Login(String username, String password)
        {
            String cookie = String.Empty;
            using (HttpClientHandler httpClientHandler = new HttpClientHandler() { UseCookies = false })
            {
                using (HttpClient client = new HttpClient(handler: httpClientHandler))
                {
                    client.DefaultRequestHeaders.Add("user-agent", "Instagram 85.0.0.21.100 Android (28/9; 380dpi; 1080x2147; OnePlus; HWEVA; OnePlus6T; qcom; en_US; 146536611)");
                    using (StringContent content = new StringContent($"guid={Guid.NewGuid()}&username={username}&password={password}&device_id=android-JDS99162&login_attempt_count=0"))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                        using (HttpResponseMessage response = client.PostAsync("https://i.instagram.com/api/v1/accounts/login/", content).Result)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                foreach (KeyValuePair<String, IEnumerable<String>> header in response.Headers)
                                {
                                    if (header.Key.ToLower().Contains("cookie"))
                                    {
                                        cookie = header.Value.ElementAt(4);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return cookie;
        }

        public static bool SendMessage(String cookie, String thread_id, String text)
        {
            using (HttpClientHandler _httpClientHandler = new HttpClientHandler() { UseCookies = false })
            {
                using (HttpClient _client = new HttpClient(handler: _httpClientHandler))
                {
                    using (StringContent _sendContent = new StringContent($"thread_ids=[{thread_id}]&text={text}"))
                    {
                        _client.DefaultRequestHeaders.Add("user-agent", "Instagram 85.0.0.21.100 Android (28/9; 380dpi; 1080x2147; OnePlus; HWEVA; OnePlus6T; qcom; en_US; 146536611)");
                        _sendContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        _client.DefaultRequestHeaders.Add("Cookie", cookie);
                        using (HttpResponseMessage responseMessage = _client.PostAsync("https://i.instagram.com/api/v1/direct_v2/threads/broadcast/text/", _sendContent).Result)
                        {
                            return responseMessage.StatusCode != (HttpStatusCode)429;
                        }
                    }
                }
            }
        }

        public static void Log(String text, ConsoleColor consoleColor, bool new_line)
        {
            Console.ForegroundColor = consoleColor;
            if (new_line)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void StartWorkers(WorkerData workerData)
        {
            for (int i = 0; i < workerData.thread_count; i++)
            {
                Thread thread = new Thread(() => {
                    while (true)
                    {
                        if (!SendMessage(workerData.cookie, workerData.thread_id, workerData.text))
                        {
                            Program.Log("[!] Rate limited!", ConsoleColor.DarkRed, true);
                            Thread.Sleep(TimeSpan.FromMinutes(5));
                        }
                    }
                });
                threads.Add(thread);
                thread.Start();
            }
        }
    }
}
