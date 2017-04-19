﻿using Discord.WebSocket;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mirai
{
    class Command
    {
        static Dictionary<string, TextCommand> Typed;
        static Dictionary<string, VoiceCommand> Voiced = new Dictionary<string, VoiceCommand>();

        internal static void Load(string Mention)
        {
            Typed = new Dictionary<string, TextCommand>
            {
                { Mention, new TextCommand(Conversation.Commands.Question, 1) },
                { "add", new TextCommand(Audio.Commands.Add, 1) },
                { "remove", new TextCommand(Audio.Commands.Remove, 1) },
                { "directadd", new TextCommand(Audio.Commands.Play, 2) },
                { "volume", new TextCommand(Audio.Commands.Volume, 2) },
                { "join", new TextCommand(Audio.Commands.Join, 2) },
                { "voice", new TextCommand(Audio.Commands.Voice, 2) }
            };

            Voiced.Clear();

            var NumberChoices = new Choices();
            for (int i = 1; i <= 50; i++)
            {
                NumberChoices.Add(i.ToString());
            }

            AddVoiced("skip", Audio.Commands.Skip, 1);

            AddVoiced("remove", Audio.Commands.Remove, 1, e =>
            {
                e.Append(NumberChoices);
            });

            AddVoiced("move", Audio.Commands.Move, 1, e =>
            {
                e.Append(NumberChoices);
                e.Append("to");
                e.Append(NumberChoices);
            });

            AddVoiced("play", Audio.Commands.Play, 1, e =>
            {
                e.Append(new Choices(PopulateSongList().ToArray()));
            });

            AddVoiced("queue", Audio.Commands.Queue, 1);

            AddVoiced("fuck you", "no u", 1);

            AddVoiced("how are you doing", "I'm fine, thank you~", 1);

            AddVoiced("shut down the program", Management.Commands.Shutdown, 2);
        }

        private static void AddVoiced(string KeyWord, string Response, int Rank, Action<GrammarBuilder> MakeGrammar = null)
            => AddVoiced(KeyWord, (u, e) => Bot.Channel().SendMessageAsync(Response, true), Rank, MakeGrammar);

        private static void AddVoiced(string KeyWord, Func<ulong, Queue<string>, Task> Action, int Rank, Action<GrammarBuilder> MakeGrammar = null)
        {
            Voiced.Add(KeyWord, new VoiceCommand(Action, Rank)
            {
                GrammarBuilder = MakeGrammar
            });
        }

        internal static Choices GetChoices()
        {
            return new Choices(Voiced.Select(KVP => 
            {
                var Builder = new GrammarBuilder(KVP.Key);
                KVP.Value.GrammarBuilder?.Invoke(Builder);
                return Builder;
            }).ToArray());
        }

        private static List<string> PopulateSongList()
        {
            var Regex = new Regex("[^a-zA-Z0-9 '-]");
            var PartList = new List<string>();
            foreach (var File in MusicSearch.SongRequestLocal.GetFiles())
            {
                var SplitArtist = File.Name.Split(new[] { " - " }, 2, StringSplitOptions.None);
                var SongName = SplitArtist.Last();

                var SplitRemix = SongName.Split(new[] { " (" }, 2, StringSplitOptions.None);
                SongName = Regex.Replace(SplitRemix[0].Split(new[] { " feat. " }, 2, StringSplitOptions.None)[0], "").Replace("  ", " ");
                if (!PartList.Contains(SongName))
                {
                    PartList.Add(SongName);
                }

                if (SplitRemix.Length != 1)
                {
                    var Remix = Regex.Replace(SplitRemix[1].Replace(" Remix)", "").Replace(" remix)", ""), "").Replace("  ", " ") + " " + SongName;
                    if (!PartList.Contains(Remix))
                    {
                        PartList.Add(Remix);
                    }
                }

                if (SplitArtist.Length != 1)
                {
                    var Artist = Regex.Replace(SplitArtist[0], "").Replace("  ", " ") + " " + SongName;
                    if (!PartList.Contains(Artist))
                    {
                        PartList.Add(Artist);
                    }
                }
            }

            foreach (var Term in System.IO.File.ReadAllText("Search.txt").Split('\n'))
            {
                if (Term.Length > 2)
                {
                    PartList.Add(Term.Trim());
                }
            }

            return PartList;
        }

        internal static TextCommand GetText(string KeyWord, int Rank)
        {
            if (Typed?.ContainsKey(KeyWord) ?? false)
            {
                var Command = Typed[KeyWord];
                if (Command.Rank <= Rank)
                {
                    return Command;
                }
            }

            return null;
        }

        internal static VoiceCommand GetVoice(string KeyWord, int Rank)
        {
            if (Voiced?.ContainsKey(KeyWord) ?? false)
            {
                var Command = Voiced[KeyWord];
                if (Command.Rank <= Rank)
                {
                    return Command;
                }
            }

            return null;
        }
    }

    abstract class ICommand<T1, T2>
    {
        internal Func<T1, T2, Task> Handler;
        internal int Rank;

        internal ICommand(Func<T1, T2, Task> Handler, int Rank)
        {
            this.Handler = Handler;
            this.Rank = Rank;
        }

        internal async void Invoke(T1 arg1, T2 arg2)
        {
            try
            {
                await Handler(arg1, arg2);
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex);
            }
        }
    }

    class TextCommand : ICommand<string, SocketMessage>
    {
        internal TextCommand(Func<string, SocketMessage, Task> Handler, int Rank) : base(Handler, Rank)
        {
        }
    }

    class VoiceCommand : ICommand<ulong, Queue<string>>
    {
        internal Action<GrammarBuilder> GrammarBuilder;

        internal VoiceCommand(Func<ulong, Queue<string>, Task> Handler, int Rank) : base(Handler, Rank)
        {
        }
    }
}
