using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ScrapingTemplateSimple4._8.Models;
using ScrapingTemplateSimple4._8.Services;

namespace ScrapingTemplateSimple4._8.Extensions
{
    public static class MultitaskingExtensions
    {
        public static void Save<T>(this List<T> items, string path = null)
        {
            var name = typeof(T).Name;
            if (path != null) name = path;
            File.WriteAllText(name, JsonConvert.SerializeObject(items));
        }

        public static List<T> Load<T>(this string path)
        {
            return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path));
        }

        public static async Task<List<T2>> Parallel<T, T2>(this IReadOnlyList<T> inputs, int threads, Func<T, Task<T2>> work,bool resume=true)
        {
            var outputs = new List<T2>();
            if (resume)
                PrepareResume(ref inputs, ref outputs);
            var isString = inputs.First() is string;
            var name = typeof(T2).Name;
            if (name == "String")
                name = "Urls";
            Notifier.Display("Start working");
            var i = 0;
            var taskUrls = new Dictionary<int, string>();
            var tasks = new List<Task<T2>>();
            Func<T, string> getter = null;
            if (!isString)
                getter = inputs.First().Getter<T, string>("Url");
            do
            {
                if (i < inputs.Count)
                {
                    var item = inputs[i];
                    string url;
                    if (item is string s)
                        url = s;
                    else
                        url = getter(item);
                    Notifier.Display($"Working on {i + 1} / {inputs.Count} , Total collected : {outputs.Count}");
                    var t = work(item);
                    taskUrls.Add(t.Id, url);
                    tasks.Add(t);
                    i++;
                }

                if (tasks.Count != threads && i < inputs.Count) continue;
                var currentTaskId = -1;
                try
                {
                    var t = await Task.WhenAny(tasks).ConfigureAwait(false);
                    currentTaskId = t.Id;
                    tasks.Remove(t);
                    outputs.Add(await t);
                }
                catch (TaskCanceledException)
                {
                    outputs.Save(name);
                    throw;
                }
                catch (KnownException ex)
                {
                    Notifier.Error($"{taskUrls[currentTaskId]}\n{ex.Message}");
                    var t = tasks.FirstOrDefault(x => x.IsFaulted);
                    tasks.Remove(t);
                }
                catch (Exception e)
                {
                    Notifier.Error($"{taskUrls[currentTaskId]}\n{e}");
                    var t = tasks.FirstOrDefault(x => x.IsFaulted);
                    tasks.Remove(t);
                }

                if (tasks.Count == 0 && i == inputs.Count) break;
            } while (true);

            outputs.Save(name);
            Notifier.Display("Work completed");
            return outputs;
        }

        public static async Task<List<T2>> Parallel<T, T2>(this IReadOnlyList<T> inputs, int threads, Func<T, Task<List<T2>>> work,bool resume=true)
        {
            var outputs = new List<T2>();
            if (resume)
                PrepareResume(ref inputs, ref outputs);
            var isString = inputs.First() is string;
            var name = typeof(T2).Name;
            if (name == "String")
                name = "Urls";
            Notifier.Display("Start working");
            var i = 0;
            var taskUrls = new Dictionary<int, string>();
            var tasks = new List<Task<List<T2>>>();
            Func<T, string> getter = null;
            if (!isString)
                getter = inputs.First().Getter<T, string>("Url");
            do
            {
                if (i < inputs.Count)
                {
                    var item = inputs[i];
                    string url;
                    if (item is string s)
                        url = s;
                    else
                        url = getter(item);
                    Notifier.Display($"Working on {i + 1} / {inputs.Count} , Total collected : {outputs.Count}");
                    var t = work(item);
                    taskUrls.Add(t.Id, url);
                    tasks.Add(t);
                    i++;
                }

                if (tasks.Count != threads && i < inputs.Count) continue;
                var currentTaskId = -1;
                try
                {
                    var t = await Task.WhenAny(tasks).ConfigureAwait(false);
                    currentTaskId = t.Id;
                    tasks.Remove(t);
                    outputs.AddRange(await t);
                }
                catch (TaskCanceledException)
                {
                    outputs.Save(name);
                    throw;
                }
                catch (KnownException ex)
                {
                    Notifier.Error($"{taskUrls[currentTaskId]}\n{ex.Message}");
                    var t = tasks.FirstOrDefault(x => x.IsFaulted);
                    tasks.Remove(t);
                }
                catch (Exception e)
                {
                    Notifier.Error($"{taskUrls[currentTaskId]}\n{e}");
                    var t = tasks.FirstOrDefault(x => x.IsFaulted);
                    tasks.Remove(t);
                }

                if (tasks.Count == 0 && i == inputs.Count) break;
            } while (true);

            outputs.Save(name);
            Notifier.Display("Work completed");
            return outputs;
        }

        private static void PrepareResume<T, T2>(ref IReadOnlyList<T> inputs, ref List<T2> outputs)
        {
            var isInputString = typeof(T) == typeof(string);
            var isOutputString = typeof(T2) == typeof(string);
            var name = isOutputString ? "Urls" : typeof(T2).Name;
            if (File.Exists(name))
                outputs = name.Load<T2>();
            if (outputs == null) throw new KnownException($"Null output on file");
            if (outputs.Count == 0) return;
            HashSet<string> collected;
            if (isOutputString)
            {
                outputs = outputs.ToHashSet().ToList();
                outputs.Save();
                collected = (outputs as List<string> ?? throw new InvalidOperationException()).ToHashSet();
            }
            else
            {
                var getter = outputs.FirstOrDefault().Getter<T2, string>("Url");
                outputs = outputs.GroupBy(getter).Select(x => x.First()).ToList();
                outputs.Save(name);
                collected = outputs.Select(getter).ToHashSet();
            }

            if (isInputString)
            {
                inputs = inputs.Where(x => !collected.Contains(x as string)).ToList();
            }
            else
            {
                var getter2 = inputs.FirstOrDefault().Getter<T, string>("Url");
                inputs = inputs.Where(x => !collected.Contains(getter2(x))).ToList();
            }

            if (inputs.Count == 0) throw new KnownException($"No input to work on, total data : {outputs.Count}");
        }
        
    }
}