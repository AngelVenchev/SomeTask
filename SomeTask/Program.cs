using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SomeTask
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var directory = Directory.GetCurrentDirectory() + @"\lists"
            //var files = Directory.GetFiles(directory);
            HashSet<string> words = new HashSet<string>();
            //foreach (var file in files)
            //{
            //    words = words.Union(
            //        File.ReadAllLines(file)
            //        .Select(x => x.ToLower().Trim())
            //        .Distinct()
            //        ).ToHashSet();
            //}
            var file = Directory.GetCurrentDirectory() + @"\lists\Aword.csv";
            words = File.ReadAllLines(file)
                .Select(x => x.ToLower()
                .Trim())
                .Distinct()
                .Where(x => x.Length == 7)
                .Take(20)
                .ToHashSet();


            var a = words.ToList();
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine(a[i]);
            }


            var game = new Game();
            game.Start(words, 7);
        }
    }

    class Game
    {
        private HashSet<string> words;
        private Feedback total = new Feedback();

        public string Answer { get; set; }
        public Feedback Feedback { get; set; }

        public void Start(HashSet<string> words, int length)
        {
            this.words = words.Where(x => x.Length == length).ToHashSet();
            this.total.Answer = new List<char>(this.words.First().Select(x => '?'));
            this.total.ExtraLetters = new HashSet<char>();

            PrintGameStart(this.words);
            while (true)
            {
                var input = Console.ReadLine();
                if (this.words.Count == 0)
                {
                    PrintSuccess();
                    break;
                }
                var inputValid = ValidateInput(input, this.words.First().Length);
                if (inputValid)
                {
                    var normalizedInput = input.Trim().ToLower();

                    Feedback currentFeedback = null;
                    Feedback totalFeedback = null;

                    int maxGrade = 0;
                    var bestMatchingWords = new List<string>();
                    foreach (var possibleWord in this.words)
                    {
                        var possibleFeedback = GenerateFeedback(normalizedInput, possibleWord);
                        Feedback possibleTotal = GenerateTotalFeedback(this.total, possibleFeedback, possibleWord);
                        var matchingWords = GetMatchingWords(possibleTotal, this.words);
                        if (matchingWords.Count > maxGrade)
                        {
                            bestMatchingWords = matchingWords;
                            maxGrade = matchingWords.Count;
                            currentFeedback = possibleFeedback;
                            totalFeedback = possibleTotal;
                            Console.WriteLine($"Checking word {possibleWord} with {matchingWords.Count} matching words");
                        }
                    }

                    this.words = bestMatchingWords.ToHashSet();

                    this.total = totalFeedback;
                    if (currentFeedback.IsSuccess())
                    {
                        PrintSuccess();
                        break;
                    }
                    else
                    {
                        PrintFeedback(currentFeedback);
                        PrintFeedback(this.total);
                    }
                }
                else
                {
                    PrintInvalidGuessFeedback(this.words);
                }
            }
        }

        private static List<string> GetMatchingWords(Feedback newTotal, HashSet<string> words)
        {
            return words.Where(x => IsWordPossibleSwitch(newTotal, x)).ToList();
        }

        private static bool IsWordPossibleSwitch(Feedback feedback, string word)
        {
            for (int i = 0; i < feedback.Answer.Count; i++)
            {
                if (feedback.Answer[i] != '?' && word[i] != feedback.Answer[i])
                {
                    return false;
                }
            }

            var wordLetterCounts = GetLetterCountForWord(word);
            var feedbackLetterCounts = GetLetterCountForWord(new string(feedback.Answer.ToArray()));

            foreach (var letter in feedback.ExtraLetters)
            {
                var wordLetterCount = wordLetterCounts.ContainsKey(letter) ? wordLetterCounts[letter] : 0;
                var feedbackLetterCount = feedbackLetterCounts.ContainsKey(letter) ? feedbackLetterCounts[letter] : 0;

                if (wordLetterCount <= feedbackLetterCount)
                {
                    return false;
                }
            }

            // Word: ALFA
            // feed: A???:AL
            // wordLetterCount = 2  // A
            // feedLetterCount = 1  // A

            return true;
        }


        private static Feedback CloneFeedback(Feedback feedback)
        {
            return new Feedback
            {
                Answer = new List<char>(feedback.Answer),
                ExtraLetters = new HashSet<char>(feedback.ExtraLetters)
            };
        }

        private static void PrintInvalidGuessFeedback(HashSet<string> words)
        {
            Console.WriteLine($"you must guess a word that is {words.First().Length} letters long");
        }

        private static void PrintSuccess()
        {
            Console.WriteLine("good! you win!");
        }

        private static void PrintGameStart(HashSet<string> words)
        {
            Console.WriteLine($"i picked a word, it has {words.First().Length} letters");
            Console.WriteLine("make a guess please");
        }

        private static Dictionary<char, int> GetLetterCountForWord(string word)
        {
            return word
                .GroupBy(x => x, x => word.Count(l => l == x))
                .ToDictionary(x => x.Key, x => x.First());
        }

        private static Feedback GenerateFeedback(string normalizedInput, string word)
        {
            var feedBackAnswer = GenerateFeedbackAnswer(normalizedInput, word.Select(x => '?').ToList(), word);
            var extraLetters = GenerateExtraLetters(normalizedInput, feedBackAnswer, word);
            return new Feedback()
            {
                Answer = feedBackAnswer,
                ExtraLetters = extraLetters
            };
        }

        private void PrintFeedback(Feedback currentFeedback)
        {
            Console.WriteLine(
                new string(currentFeedback.Answer.ToArray()) +
                ':' +
                new string(currentFeedback.ExtraLetters.ToList().OrderBy(x => x).ToArray()));
        }

        private static Feedback GenerateTotalFeedback(Feedback oldFeedback, Feedback newFeedback, string word)
        {
            var newTotal = CloneFeedback(oldFeedback);

            for (int i = 0; i < newFeedback.Answer.Count; i++)
            {
                if (newFeedback.Answer[i] != '?')
                {
                    newTotal.Answer[i] = newFeedback.Answer[i];
                }
            }
            foreach (var letter in newFeedback.ExtraLetters)
            {
                newTotal.ExtraLetters.Add(letter);
            }

            HashSet<char> hidden = new HashSet<char>();
            for (int i = 0; i < newTotal.Answer.Count; i++)
            {
                if (newTotal.Answer[i] == '?')
                {
                    hidden.Add(word[i]);
                }
            }
            newTotal.ExtraLetters = newTotal.ExtraLetters.Intersect(hidden).ToHashSet();

            return newTotal;
        }

        private static HashSet<char> GenerateExtraLetters(string normalizedInput, List<char> feedBackAnswer, string word)
        {
            HashSet<char> extraLetters = new HashSet<char>();
            var inputLettersCount = GetLetterCountForWord(normalizedInput);
            var answerLettersCount = GetLetterCountForWord(new string(feedBackAnswer.ToArray()));
            var wordLettersCounts = GetLetterCountForWord(word);

            foreach (var inputLetterCount in inputLettersCount)
            {
                var currentLetter = inputLetterCount.Key;
                var letterCountInWord = wordLettersCounts.ContainsKey(currentLetter) ?
                    wordLettersCounts[currentLetter] : 0;
                var letterCountInAnswer = answerLettersCount.ContainsKey(currentLetter) ?
                    answerLettersCount[currentLetter] : 0;

                if (letterCountInWord - letterCountInAnswer > 0)
                {
                    extraLetters.Add(currentLetter);
                }
            }

            return extraLetters;
        }

        private static List<char> GenerateFeedbackAnswer(string normalizedInput, List<char> feedbackAnswer, string word)
        {
            var answer = new List<char>(feedbackAnswer);
            for (int i = 0; i < normalizedInput.Length; i++)
            {
                if (word[i] == normalizedInput[i])
                {
                    answer[i] = normalizedInput[i];
                }
            }
            return answer;
        }

        private bool ValidateInput(string input, int length)
        {
            return input.Trim().ToLower().Length == length;
        }
    }

    public class Feedback
    {
        public List<char> Answer { get; set; }
        public HashSet<char> ExtraLetters { get; set; }

        public bool IsSuccess()
        {
            return !Answer.Contains('?');
        }
    }
}
