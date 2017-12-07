﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antura.Helpers;
using Antura.Teacher;

namespace Antura.Database
{

    /// <summary>
    /// Provides helpers to get correct letter/word/phrase data according to the teacher's logic and based on the player's progression
    /// </summary>
    public class VocabularyHelper
    {
        private DatabaseManager dbManager;

        // HACK: this is needed for some games where LamAlef behaves differently
        public bool ForceUnseparatedLetters { get; set; }

        // @note: these words are problematic as they contain letters that are not available in the journey position they are played at
        // TODO: remove this! Should be handled by the curriculum
        public List<string> ProblematicWordIds = new List<string>();
        /*
        {
            "won",
            "went",
            "sat",
            "studied",
            "laughed",
            "played",
            "flapped",
            "caught",
            "released",
            "carried",
            "understood"
        };*/

        public VocabularyHelper(DatabaseManager _dbManager)
        {
            dbManager = _dbManager;
        }

        #region Letter Utilities

        private bool CheckFilters(LetterFilters filters, LetterData data)
        {
            if (filters.requireDiacritics && !data.IsOfKindCategory(LetterKindCategory.DiacriticCombo)) { return false; }
            if (!FilterByDiacritics(filters.excludeDiacritics, data)) { return false; }
            if (!FilterByLetterVariations(filters.excludeLetterVariations, data)) { return false; }
            if (!FilterByDipthongs(filters.excludeDiphthongs, data)) { return false; }

            // always skip symbols
            if (data.IsOfKindCategory(LetterKindCategory.Symbol)) {
                return false;
            }
            return true;
        }


        public bool FilterByDiacritics(LetterFilters.ExcludeDiacritics excludeDiacritics, LetterData data)
        {
            switch (excludeDiacritics) {
                case LetterFilters.ExcludeDiacritics.All:
                    if (data.IsOfKindCategory(LetterKindCategory.DiacriticCombo)) {
                        return false;
                    }
                    break;
                case LetterFilters.ExcludeDiacritics.AllButMain:
                    var symbol = GetSymbolOf(data.Id);
                    if (symbol != null && data.IsOfKindCategory(LetterKindCategory.DiacriticCombo) &&
                        symbol.Tag != "MainDiacritic") {
                        return false;
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool FilterByLetterVariations(LetterFilters.ExcludeLetterVariations excludeLetterVariations, LetterData data)
        {
            switch (excludeLetterVariations) {
                case LetterFilters.ExcludeLetterVariations.All:
                    if (data.IsOfKindCategory(LetterKindCategory.LetterVariation)) {
                        return false;
                    }
                    break;
                case LetterFilters.ExcludeLetterVariations.AllButAlefHamza:
                    if (data.IsOfKindCategory(LetterKindCategory.LetterVariation) && data.Tag != "AlefHamzaVariation") {
                        return false;
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool FilterByDipthongs(bool excludeDiphthongs, LetterData data)
        {
            if (excludeDiphthongs && data.Kind == LetterDataKind.Diphthong) {
                return false;
            }
            return true;
        }

        #endregion

        #region Letter -> Letter

        public List<LetterData> GetAllBaseLetters()
        {
            var p = new LetterFilters(excludeDiacritics: LetterFilters.ExcludeDiacritics.All,
                excludeLetterVariations: LetterFilters.ExcludeLetterVariations.All, excludeDiphthongs: true);
            return GetAllLetters(p);
        }

        public List<LetterData> GetAllLetters(LetterFilters filters)
        {
            return dbManager.FindLetterData(x => CheckFilters(filters, x));
        }

        private List<LetterData> GetLettersNotIn(List<string> tabooList, LetterFilters filters)
        {
            return dbManager.FindLetterData(x => !tabooList.Contains(x.Id) && CheckFilters(filters, x));
        }

        public List<LetterData> GetLettersNotIn(LetterFilters filters, params LetterData[] tabooArray)
        {
            var tabooList = new List<LetterData>(tabooArray);
            return GetLettersNotIn(tabooList.ConvertAll(x => x.Id), filters);
        }

        public List<LetterData> GetLettersByKind(LetterDataKind choice)
        {
            return dbManager.FindLetterData(x => x.Kind == choice); // @note: this does not use filters, special case
        }

        public List<LetterData> GetLettersBySunMoon(LetterDataSunMoon choice, LetterFilters filters)
        {
            return dbManager.FindLetterData(x => x.SunMoon == choice && CheckFilters(filters, x));
        }

        public List<LetterData> GetConsonantLetter(LetterFilters filters)
        {
            return dbManager.FindLetterData(x =>
                x.Type == LetterDataType.Consonant || x.Type == LetterDataType.Powerful && CheckFilters(filters, x));
        }

        public List<LetterData> GetVowelLetter(LetterFilters filters)
        {
            return dbManager.FindLetterData(x => x.Type == LetterDataType.LongVowel && CheckFilters(filters, x));
        }

        public List<LetterData> GetLettersByType(LetterDataType choice, LetterFilters filters)
        {
            return dbManager.FindLetterData(x => x.Type == choice && CheckFilters(filters, x));
        }

        public LetterData GetBaseOf(string letterId)
        {
            var data = dbManager.GetLetterDataById(letterId);
            if (data.BaseLetter == "") {
                return null;
            }
            return dbManager.FindLetterData(x => x.Id == data.BaseLetter)[0];
        }

        public LetterData GetSymbolOf(string letterId)
        {
            var data = dbManager.GetLetterDataById(letterId);
            if (data.Symbol == "") {
                return null;
            }
            return dbManager.FindLetterData(x => x.Id == data.Symbol)[0];
        }

        public List<LetterData> GetLettersWithBase(string letterId)
        {
            var baseData = dbManager.GetLetterDataById(letterId);
            return dbManager.FindLetterData(x => x.BaseLetter == baseData.Id);
        }

        public List<LetterData> ConvertToLettersWithForcedForms(LetterData baseForVariation)
        {
            return new List<LetterForm>(baseForVariation.GetAvailableForms()).ConvertAll(f =>
            {
                var l = baseForVariation.Clone();
                l.ForcedLetterForm = f;
                return l;
            });
        }

        public LetterData ConvertToLetterWithForcedForm(LetterData baseForVariation, LetterForm form)
        {
            var l = baseForVariation.Clone();
            l.ForcedLetterForm = form;
            return l;
        }

        #endregion

        #region Word -> Letter

        private Dictionary<string, List<LetterData>> unseparatedWordsToLetterCache = new Dictionary<string, List<LetterData>>();
        private Dictionary<string, List<LetterData>> separatedWordsToLetterCache = new Dictionary<string, List<LetterData>>();

        /*private List<string> GetLetterIdsInWordData(WordData wordData)
        {
            List<string> letter_ids_list = null;
            if (ForceUnseparatedLetters)
            {
                if (!unseparatedWordsToLetterCache.ContainsKey(wordData.Id))
                {
                    var parts = ArabicAlphabetHelper.AnalyzeData(AppManager.I.DB.StaticDatabase, wordData, separateVariations: false);
                    letter_ids_list = parts.ConvertAll(p => p.letter.Id);
                    unseparatedWordsToLetterCache[wordData.Id] = letter_ids_list;
                }
                letter_ids_list = unseparatedWordsToLetterCache[wordData.Id];
            } else {
                letter_ids_list = new List<string>(wordData.Letters);
            }
            return letter_ids_list;
        }*/

        public List<LetterData> GetLettersInWord(string wordId)
        {
            WordData wordData = dbManager.GetWordDataById(wordId);
            return GetLettersInWord(wordData);
        }
        public List<LetterData> GetLettersInWord(WordData wordData)
        {
            // @note: this will always retrieve all letters with their forms, the strictness will then define whether that has any consequence or not
            List<LetterData> letters = null;
            var dictCache = ForceUnseparatedLetters ? unseparatedWordsToLetterCache : separatedWordsToLetterCache;
            bool separateVariations = !ForceUnseparatedLetters;
            if (!dictCache.ContainsKey(wordData.Id))
            {
                var parts = ArabicAlphabetHelper.AnalyzeData(dbManager.StaticDatabase, wordData, separateVariations: separateVariations);
                letters = parts.ConvertAll(x => ConvertToLetterWithForcedForm(x.letter, x.letterForm));
                dictCache[wordData.Id] = letters;
            }
            letters = dictCache[wordData.Id];
            return letters;
        }


        public List<LetterData> GetLettersNotInWords(params WordData[] tabooArray)
        {
            return GetLettersNotInWords(LetterKindCategory.Real, tabooArray);
        }

        public List<LetterData> GetLettersNotInWords(LetterKindCategory category = LetterKindCategory.Real, params WordData[] tabooArray)
        {
            // TODO: make sure that all uses of LetterData work with strict or non-strict!
            var lettersInWords = new HashSet<LetterData>();
            foreach (var tabooWordData in tabooArray) {
                var tabooWordDataLetters = GetLettersInWord(tabooWordData);
                lettersInWords.UnionWith(tabooWordDataLetters);
            }
            var lettersNotInWords = dbManager.FindLetterData(x => !lettersInWords.Contains(x) && x.IsOfKindCategory(category));
            return lettersNotInWords;
        }

        public List<LetterData> GetLettersNotInWord(WordData wordData, LetterKindCategory category = LetterKindCategory.Real)
        {
            var lettersInWord = GetLettersInWord(wordData);
            var lettersNotInWord = dbManager.FindLetterData(x => !lettersInWord.Contains(x) && x.IsOfKindCategory(category));
            return lettersNotInWord;
        }

        public List<LetterData> GetCommonLettersInWords(LetterEqualityStrictness letterEqualityStrictness, params WordData[] words)
        {
            // TODO: use letterEqualityStrictness!
            Dictionary<LetterData, int> countDict = new Dictionary<LetterData, int>();
            foreach (var word in words)
            {
                var nonRepeatingLettersOfWord = new HashSet<LetterData>();

                var letters = GetLettersInWord(word);
                foreach (var letter in letters)
                {
                    nonRepeatingLettersOfWord.Add(letter);
                }

                foreach (var letter in nonRepeatingLettersOfWord) {
                    if (!countDict.ContainsKey(letter)) countDict[letter] = 0;
                    countDict[letter] += 1;
                }
            }

            // Get only these letters that are in all words
            var commonLettersList = new List<LetterData>();
            foreach (var letter in countDict.Keys) {
                if (countDict[letter] == words.Length) {
                    commonLettersList.Add(letter);
                }
            }

            return commonLettersList;
        }

        #endregion

        #region Word Utilities

        private bool CheckFilters(WordFilters filters, WordData data)
        {
            if (filters.excludeArticles && data.Article != WordDataArticle.None) {
                return false;
            }
            if (filters.requireDrawings && !data.HasDrawing()) {
                return false;
            }
            if (filters.excludeColorWords && data.Category == WordDataCategory.Color) {
                return false;
            }
            if (filters.excludePluralDual && data.Form != WordDataForm.Singular) {
                return false;
            }
            if (filters.excludeDiacritics && this.WordHasDiacriticCombo(data)) {
                return false;
            }
            if (filters.excludeLetterVariations && this.WordHasLetterVariations(data)) {
                return false;
            }
            if (filters.requireDiacritics && !this.WordHasDiacriticCombo(data)) {
                return false;
            }
            return true;
        }

        private bool WordHasDiacriticCombo(WordData data)
        {
            foreach (var letter in GetLettersInWord(data)) {
                if (letter.IsOfKindCategory(LetterKindCategory.DiacriticCombo)) {
                    return true;
                }
            }
            return false;
        }

        private bool WordHasLetterVariations(WordData data)
        {
            foreach (var letter in GetLettersInWord(data)) {
                if (letter.IsOfKindCategory(LetterKindCategory.LetterVariation)) {
                    return true;
                }
            }
            return false;
        }

        public int WordContainsLetterTimes(WordData selectedWord, LetterData containedLetter, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly)
        {
            var wordLetters = GetLettersInWord(selectedWord);
            int count = 0;
            foreach (var letter in wordLetters)
                if (letter.IsSameLetterAs(containedLetter, letterEqualityStrictness))
                    count++;
            return count;
        }

        public bool WordContainsLetter(WordData selectedWord, LetterData containedLetter, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly)
        {
            //if (containedLetter.Id == "lam_alef") Debug.Log("Looking for lam-alef in " + selectedWord);
            //foreach (var l in ArabicAlphabetHelper.FindLetter(dbManager, selectedWord, containedLetter))
            //if (l.letter.Id == "lam_alef") Debug.Log("Lam alef form " + l.letterForm + " in word " + selectedWord);
            var lettersInWord = GetLettersInWord(selectedWord);
            return lettersInWord.Any(x => x.IsSameLetterAs(containedLetter, letterEqualityStrictness));
        }

        /// <summary>
        /// tranformsf the hex string of the glyph into the corresponding char
        /// </summary>
        /// <returns>The drawing string</returns>
        /// <param name="word">WordData.</param>
        public string GetWordDrawing(WordData word)
        {
            //Debug.Log("the int of hex:" + word.Drawing + " is " + int.Parse(word.Drawing, NumberStyles.HexNumber));
            if (word.Drawing != "") {
                int result = 0;
                if (int.TryParse(word.Drawing, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result)) {
                    return ((char)result).ToString();
                }
                return "";
            }
            return "";
        }

        #endregion

        #region Word -> Word

        public List<WordData> GetAllWords(WordFilters filters)
        {
            return dbManager.FindWordData(x => CheckFilters(filters, x));
        }

        private List<WordData> GetWordsNotIn(WordFilters filters, List<WordData> tabooWords)
        {
            return dbManager.FindWordData(word => !tabooWords.Contains(word) && CheckFilters(filters, word));
        }

        public List<WordData> GetWordsNotIn(WordFilters filters, params WordData[] tabooWords)
        {
            var tabooList = new List<WordData>(tabooWords);
            return GetWordsNotIn(filters, tabooList);
        }

        public List<WordData> GetWordsByCategory(WordDataCategory choice, WordFilters filters)
        {
            if (choice == WordDataCategory.None) return this.GetAllWords(filters);
            return dbManager.FindWordData(x => x.Category == choice && CheckFilters(filters, x));
        }

        public List<WordData> GetWordsByArticle(WordDataArticle choice, WordFilters filters)
        {
            return dbManager.FindWordData(x => x.Article == choice && CheckFilters(filters, x));
        }

        public List<WordData> GetWordsByForm(WordDataForm choice, WordFilters filters)
        {
            return dbManager.FindWordData(x => x.Form == choice && CheckFilters(filters, x));
        }

        public List<WordData> GetWordsByKind(WordDataKind choice, WordFilters filters)
        {
            return dbManager.FindWordData(x => x.Kind == choice && CheckFilters(filters, x));
        }

        #endregion

        #region Letter -> Word

        public List<WordData> GetWordsWithLetter(WordFilters filters, LetterData okLetter, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly)
        {
            return GetWordsByLetters(filters, new[] { okLetter }, null, letterEqualityStrictness);
        }

        public List<WordData> GetWordsWithLetters(WordFilters filters, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly, params LetterData[] okLetters)
        {
            return GetWordsByLetters(filters, okLetters, null, letterEqualityStrictness);
        }

        public List<WordData> GetWordsWithoutLetter(WordFilters filters, LetterData tabooLetter, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly)
        {
            return GetWordsByLetters(filters, null, new[] { tabooLetter }, letterEqualityStrictness);
        }

        public List<WordData> GetWordsWithoutLetters(WordFilters filters, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly, params LetterData[] tabooLetters)
        {
            return GetWordsByLetters(filters, null, tabooLetters, letterEqualityStrictness);
        }

        private List<WordData> GetWordsByLetters(WordFilters filters, LetterData[] okLettersArray, LetterData[] tabooLettersArray, LetterEqualityStrictness letterEqualityStrictness = LetterEqualityStrictness.LetterOnly)
        {
            if (okLettersArray == null) okLettersArray = new LetterData[] { };
            if (tabooLettersArray == null) tabooLettersArray = new LetterData[] { };

            var okLetters = new HashSet<LetterData>(okLettersArray);
            var tabooLetters = new HashSet<LetterData>(tabooLettersArray);

            List<WordData> wordsByLetters = dbManager.FindWordData(word => {
                if (!CheckFilters(filters, word)) { return false; }

                var lettersInWord = GetLettersInWord(word);

                if (tabooLetters.Count > 0) {
                    foreach (var letter in lettersInWord) {
                        if (tabooLetters.Contains(letter)) {
                            return false;
                        }
                    }
                }

                if (okLetters.Count > 0) {
                    bool hasAllOkLetters = true;
                    foreach (var okLetter in okLetters) {
                        bool hasThisLetter = false;
                        foreach (var letter in lettersInWord) {
                            if (letter.IsSameLetterAs(okLetter, letterEqualityStrictness))
                            {
                                hasThisLetter = true;
                                break;
                            }
                        }
                        if (!hasThisLetter) {
                            hasAllOkLetters = false;
                            break;
                        }
                    }
                    if (!hasAllOkLetters) return false;
                }
                return true;
            }
            );
            return wordsByLetters;
        }

        public bool WordContainsAnyLetter(WordData word, IEnumerable<LetterData> letters)
        {
            var containedLetters = GetLettersInWord(word);
            foreach (var letter in letters) {
                if (containedLetters.Contains(letter)) {
                    return true;
                }
            }
            return false;
        }

        public bool WordHasAllLettersInCommonWith(WordData word, List<WordData> words)
        {
            var lettersInWord = GetLettersInWord(word);
            foreach (var letter in lettersInWord) {
                if (!IsLetterContainedInAnyWord(letter, words)) {
                    return false;
                }
            }
            return true;
        }

        public bool IsLetterContainedInAnyWord(LetterData letter, List<WordData> words)
        {
            foreach (var word in words) {
                var containedLetters = GetLettersInWord(word);
                if (containedLetters.Contains(letter)) {
                    return true;
                }
            }
            return false;
        }

        public bool AnyWordContainsLetter(LetterData letter, IEnumerable<WordData> words)
        {
            foreach (var word in words) {
                if (GetLettersInWord(word).Contains(letter)) {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Phrase -> Word

        /// <summary>
        /// Gets the words in phrase, taken from field Words of data Pharse. these words are set manually in the db
        /// </summary>
        /// <returns>The words in phrase.</returns>
        /// <param name="phraseId">Phrase identifier.</param>
        /// <param name="wordFilters">Word filters.</param>
        public List<WordData> GetWordsInPhrase(string phraseId, WordFilters wordFilters = null)
        {
            if (wordFilters == null) { wordFilters = new WordFilters(); }
            var phraseData = dbManager.GetPhraseDataById(phraseId);
            return GetWordsInPhrase(phraseData, wordFilters);
        }

        public List<WordData> GetWordsInPhrase(PhraseData phraseData, WordFilters wordFilters)
        {
            var words_ids_list = new List<string>(phraseData.Words);
            var inputList = dbManager.FindWordData(x => words_ids_list.Contains(x.Id) && CheckFilters(wordFilters, x));
            var orderedOutputList = new List<WordData>();
            words_ids_list.ForEach(id => {
                var word = inputList.Find(x => x.Id.Equals(id));
                if (word != null) {
                    orderedOutputList.Add(word);
                }
            });
            return orderedOutputList;
        }

        public List<WordData> GetAnswersToPhrase(PhraseData phraseData, WordFilters wordFilters)
        {
            var words_ids_list = new List<string>(phraseData.Answers);
            var list = dbManager.FindWordData(x => words_ids_list.Contains(x.Id) && CheckFilters(wordFilters, x));
            return list;
        }

        #endregion

        #region Phrase filters

        private bool CheckFilters(WordFilters wordFilters, PhraseFilters phraseFilters, PhraseData data)
        {
            // Words are checked with filters. At least 1 must fulfill the requirement.
            var words = GetWordsInPhrase(data, wordFilters);
            int nOkWords = words.Count;

            var answers = GetAnswersToPhrase(data, wordFilters);
            int nOkAnswers = answers.Count;

            if (phraseFilters.requireWords && nOkWords == 0) {
                return false;
            }
            if (phraseFilters.requireAtLeastTwoWords && nOkWords <= 1) {
                return false;
            }
            if (phraseFilters.requireAnswersOrWords && nOkAnswers == 0 && nOkWords == 0) {
                return false;
            }

            return true;
        }

        #endregion

        #region Phrase -> Phrase

        public List<PhraseData> GetAllPhrases(WordFilters wordFilters, PhraseFilters phraseFilters)
        {
            return dbManager.FindPhraseData(x => CheckFilters(wordFilters, phraseFilters, x));
        }

        public List<PhraseData> GetPhrasesByCategory(PhraseDataCategory choice, WordFilters wordFilters, PhraseFilters phraseFilters)
        {
            return dbManager.FindPhraseData(x => x.Category == choice && CheckFilters(wordFilters, phraseFilters, x));
        }

        public List<PhraseData> GetPhrasesNotIn(WordFilters wordFilters, PhraseFilters phraseFilters, params PhraseData[] tabooArray)
        {
            var tabooList = new List<PhraseData>(tabooArray);
            return dbManager.FindPhraseData(x => !tabooList.Contains(x) && CheckFilters(wordFilters, phraseFilters, x));
        }

        public PhraseData GetLinkedPhraseOf(string startPhraseId)
        {
            var data = dbManager.GetPhraseDataById(startPhraseId);
            return GetLinkedPhraseOf(data);
        }

        public PhraseData GetLinkedPhraseOf(PhraseData data)
        {
            if (data.Linked == "") { return null; }
            return dbManager.FindPhraseData(x => x.Id == data.Linked)[0];
        }

        #endregion

        #region Word -> Phrase

        public List<PhraseData> GetPhrasesWithWords(params string[] okWordsArray)
        {
            if (okWordsArray == null) { okWordsArray = new string[] { }; }

            var okWords = new HashSet<string>(okWordsArray);

            var phrasesList = dbManager.FindPhraseData(x => {
                if (okWords.Count > 0) {
                    bool hasAllOkWords = true;
                    foreach (var okWord in okWords) {
                        bool hasThisWord = false;
                        foreach (var word_id in x.Words) {
                            if (word_id == okWord) {
                                hasThisWord = true;
                                break;
                            }
                        }
                        if (!hasThisWord) {
                            hasAllOkWords = false;
                            break;
                        }
                    }
                    if (!hasAllOkWords) { return false; }
                }
                return true;
            }
            );
            return phrasesList;
        }

        #endregion

    }
}