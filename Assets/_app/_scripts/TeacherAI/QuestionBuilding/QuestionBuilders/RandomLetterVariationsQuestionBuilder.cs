using System.Collections.Generic;
using Antura.Core;
using Antura.Database;
using Antura.Helpers;
using UnityEngine;

namespace Antura.Teacher
{
    public class LetterVariationFilters
    {
        public LetterFilters.ExcludeDiacritics ExcludeDiacritics = LetterFilters.ExcludeDiacritics.None;
        public LetterFilters.ExcludeLetterVariations ExcludeLetterVariations = LetterFilters.ExcludeLetterVariations.None;
        public bool excludeDipthongs = false;
        public bool excludeForms = false;
        //public bool excludeBaseLetter = false;
    }


    /// <summary>
    /// Selects variations of one letter at random
    /// * Question: The variation to find
    /// * Correct answers: The correct variation
    /// * Wrong answers: Wrong variations
    /// </summary>
    public class RandomLetterVariationsQuestionBuilder : IQuestionBuilder
    {
        // focus: Letters of different 
        // pack history filter: parameterized
        // journey: enabled

        private int nPacks;
        private int nCorrect;
        private int nWrong;
        private bool firstCorrectIsQuestion;
        private QuestionBuilderParameters parameters;
        private LetterVariationFilters letterVariationFilters;

        public QuestionBuilderParameters Parameters
        {
            get { return this.parameters; }
        }

        public RandomLetterVariationsQuestionBuilder(int nPacks, int nCorrect = 1, int nWrong = 0, 
            bool firstCorrectIsQuestion = false,
            LetterVariationFilters letterVariationFilters = null,
            QuestionBuilderParameters parameters = null)
        {
            if (letterVariationFilters == null) letterVariationFilters = new LetterVariationFilters();

            if (parameters == null)
            {
                parameters = new QuestionBuilderParameters();
            }

            this.nPacks = nPacks;
            this.nCorrect = nCorrect;
            this.nWrong = nWrong;
            this.firstCorrectIsQuestion = firstCorrectIsQuestion;
            this.parameters = parameters;
            this.letterVariationFilters = letterVariationFilters;

            // Forced filters, we need only base letters as the basis here
            this.parameters.letterFilters.excludeDiacritics = LetterFilters.ExcludeDiacritics.All;
            this.parameters.letterFilters.excludeLetterVariations = LetterFilters.ExcludeLetterVariations.All;
            this.parameters.letterFilters.excludeDiphthongs = true;
        }

        private List<string> previousPacksIDs = new List<string>();

        public List<QuestionPackData> CreateAllQuestionPacks()
        {
            previousPacksIDs.Clear();

            var packs = new List<QuestionPackData>();
            for (int pack_i = 0; pack_i < nPacks; pack_i++)
            {
                var pack = CreateSingleQuestionPackData();
                packs.Add(pack);
            }
            return packs;
        }

        private QuestionPackData CreateSingleQuestionPackData()
        {
            var teacher = AppManager.I.Teacher;
            var vocabularyHelper = AppManager.I.VocabularyHelper;

            // First, choose a letter (only from base letters, due to letter filters)
            var chosenLetters = teacher.VocabularyAi.SelectData(
                () => vocabularyHelper.GetAllLetters(parameters.letterFilters),
                    new SelectionParameters(parameters.correctSeverity, 1, useJourney: parameters.useJourneyForCorrect,
                        packListHistory: parameters.correctChoicesHistory, filteringIds: previousPacksIDs)
            );
            var baseLetter = chosenLetters[0];

            // Then, find all the different variations
            var letterPool = new List<LetterData>();

            //if (!letterVariationFilters.excludeBaseLetter)
            //{
            //letterPool.Add(baseLetter);
            //}

            var availableVariations = vocabularyHelper.GetLettersWithBase(baseLetter.GetId());
            foreach (var letterData in availableVariations)
            {
                if (!vocabularyHelper.FilterByDiacritics(letterVariationFilters.ExcludeDiacritics, letterData)) continue;
                if (!vocabularyHelper.FilterByLetterVariations(letterVariationFilters.ExcludeLetterVariations, letterData)) continue;
                if (!vocabularyHelper.FilterByDipthongs(letterVariationFilters.excludeDipthongs, letterData)) continue;
                letterPool.Add(letterData);
            }

            if (!letterVariationFilters.excludeForms)
            {
                List<LetterData> basesForVariations = new List<LetterData>(letterPool);
                //if (!basesForVariations.Contains(baseLetter))
                basesForVariations.Add(baseLetter);
                foreach (var baseForVariation in basesForVariations)
                {
                    var availableForms = new List<LetterForm>(baseForVariation.GetAvailableForms()).ConvertAll(f =>
                    {
                        var l = baseForVariation.Clone();
                        l.ForcedLetterForm = f;
                        return l;
                    });
                    letterPool.AddRange(availableForms);
                }
            }

            var correctVariations = letterPool.RandomSelect(nCorrect);
            var wrongVariations = letterPool;
            foreach (LetterData data in correctVariations)
                wrongVariations.Remove(data);
            wrongVariations = wrongVariations.RandomSelect(Mathf.Min(nWrong,wrongVariations.Count));

            var question = baseLetter;
            if (firstCorrectIsQuestion) question = correctVariations[0];

            if (ConfigAI.VerboseQuestionPacks)
            {
                string debugString = "--------- TEACHER: question pack result ---------";
                debugString += "\nQuestion letter: " + question;
                debugString += "\nCorrect Variations: " + correctVariations.Count;
                foreach (var l in correctVariations) debugString += " " + l;
                debugString += "\nWrong Variations: " + wrongVariations.Count;
                foreach (var l in wrongVariations) debugString += " " + l;
                ConfigAI.AppendToTeacherReport(debugString);
            }

            return QuestionPackData.Create(question, correctVariations, wrongVariations);
        }

    }
}