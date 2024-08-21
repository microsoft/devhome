// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using DevHome.Service.Types;

namespace DevHome.Service;

[ComVisible(true)]
[Guid("1F98F450-C163-4A99-B257-E1E6CB3E1C57")]
[ComDefaultInterface(typeof(ITimServer))]
public sealed class ProcessNotificationService : ITimServer
{
    public ProcessNotificationService()
    {
    }

    public string GetJokeInternal()
    {
        Joke joke = _jokes.ElementAt(
            Random.Shared.Next(_jokes.Count));

        return $"{joke.Setup}{Environment.NewLine}{joke.Punchline}";
    }

    int ITimServer.GetNumber()
    {
        return 48;
    }

    // Programming jokes borrowed from:
    // https://github.com/eklavyadev/karljoke/blob/main/source/jokes.json
    private readonly HashSet<Joke> _jokes = new()
    {
        new Joke("What's the best thing about a Boolean?", "Even if you're wrong, you're only off by a bit."),
        new Joke("What's the object-oriented way to become wealthy?", "Inheritance"),
        new Joke("Why did the programmer quit their job?", "Because they didn't get arrays."),
        new Joke("Why do programmers always mix up Halloween and Christmas?", "Because Oct 31 == Dec 25"),
        new Joke("How many programmers does it take to change a lightbulb?", "None that's a hardware problem"),
        new Joke("If you put a million monkeys at a million keyboards, one of them will eventually write a Java program", "the rest of them will write Perl"),
        new Joke("['hip', 'hip']", "(hip hip array)"),
        new Joke("To understand what recursion is...", "You must first understand what recursion is"),
        new Joke("There are 10 types of people in this world...", "Those who understand binary and those who don't"),
        new Joke("Which song would an exception sing?", "Can't catch me - Avicii"),
        new Joke("Why do Java programmers wear glasses?", "Because they don't C#"),
        new Joke("How do you check if a webpage is HTML5?", "Try it out on Internet Explorer"),
        new Joke("A user interface is like a joke.", "If you have to explain it then it is not that good."),
        new Joke("I was gonna tell you a joke about UDP...", "...but you might not get it."),
        new Joke("The punchline often arrives before the set-up.", "Do you know the problem with UDP jokes?"),
        new Joke("Why do C# and Java developers keep breaking their keyboards?", "Because they use a strongly typed language."),
        new Joke("Knock-knock.", "A race condition. Who is there?"),
        new Joke("What's the best part about TCP jokes?", "I get to keep telling them until you get them."),
        new Joke("A programmer puts two glasses on their bedside table before going to sleep.", "A full one, in case they gets thirsty, and an empty one, in case they don’t."),
        new Joke("There are 10 kinds of people in this world.", "Those who understand binary, those who don't, and those who weren't expecting a base 3 joke."),
        new Joke("What did the router say to the doctor?", "It hurts when IP."),
        new Joke("An IPv6 packet is walking out of the house.", "He goes nowhere."),
        new Joke("3 SQL statements walk into a NoSQL bar. Soon, they walk out", "They couldn't find a table."),
    };
}

public readonly record struct Joke(string Setup, string Punchline);

/*
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D1FF65D2-7CDA-489E-9AE0-701855C4F6A1")]
public interface ITimServer
{
    int GetNumber(out int number);
}
*/
