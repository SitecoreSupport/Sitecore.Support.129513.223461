using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using ConditionsUtility = Sitecore.Support.Rules.Conditions.ConditionsUtility;

namespace Sitecore.Support.Analytics.Rules.Conditions
{
  public abstract class HasEventOccurredCondition<T> : WhenCondition<T> where T : RuleContext
  {
    private readonly bool filterByCustomData;

    protected HasEventOccurredCondition(bool filterByCustomData)
    {
      this.filterByCustomData = filterByCustomData;
    }

    public string CustomData { get; set; }

    public string CustomDataOperatorId { get; set; }

    public int NumberOfElapsedDays { get; set; }

    public string NumberOfElapsedDaysOperatorId { get; set; }

    public int NumberOfPastInteractions { get; set; }

    public string NumberOfPastInteractionsOperatorId { get; set; }

    protected virtual IEnumerable<KeyBehaviorCacheEntry> FilterKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache)
    {
      Assert.ArgumentNotNull(keyBehaviorCache, "keyBehaviorCache");
      var behaviorCacheEntries =
        FilterKeyBehaviorCacheEntriesByInteractionConditions(
          keyBehaviorCache.Campaigns.Concat(keyBehaviorCache.Channels)
            .Concat(keyBehaviorCache.CustomValues)
            .Concat(keyBehaviorCache.Goals)
            .Concat(keyBehaviorCache.Outcomes)
            .Concat(keyBehaviorCache.PageEvents)
            .Concat(keyBehaviorCache.Venues));
      if (!filterByCustomData)
        return
          Assert.ResultNotNull(GetKeyBehaviorCacheEntries(keyBehaviorCache)
            .Intersect(behaviorCacheEntries, new KeyBehaviorCacheEntry.KeyBehaviorCacheEntryEqualityComparer()));
      if (CustomData == null)
      {
        Log.Warn("CustomData can not be null", GetType());
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      }
      behaviorCacheEntries = behaviorCacheEntries.Where(entry =>
      {
        return entry.Data != null && ConditionsUtility.CompareStrings(entry.Data, CustomData, CustomDataOperatorId);
      });
      return
        Assert.ResultNotNull(GetKeyBehaviorCacheEntries(keyBehaviorCache)
          .Intersect(behaviorCacheEntries, new KeyBehaviorCacheEntry.KeyBehaviorCacheEntryEqualityComparer()));
    }

    protected virtual IEnumerable<KeyBehaviorCacheEntry> FilterKeyBehaviorCacheEntriesByInteractionConditions(
      IEnumerable<KeyBehaviorCacheEntry> keyBehaviorCacheEntries)
    {
      Assert.ArgumentNotNull(keyBehaviorCacheEntries, "keyBehaviorCacheEntries");
      if (ConditionsUtility.GetInt32Comparer(NumberOfElapsedDaysOperatorId) == null)
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      var numberOfPastInteractionsComparer = ConditionsUtility.GetInt32Comparer(NumberOfPastInteractionsOperatorId);
      var numberOfElapsedDaysOperatorsComparer = ConditionsUtility.GetInt32Comparer(NumberOfElapsedDaysOperatorId);
      if (numberOfPastInteractionsComparer == null)
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      return Assert.ResultNotNull(keyBehaviorCacheEntries.GroupBy(entry => new
      {
        entry.InteractionId,
        entry.InteractionStartDateTime
      }).OrderByDescending(entries => entries.Key.InteractionStartDateTime).Where((entries, i) =>
      {
        if (numberOfElapsedDaysOperatorsComparer((DateTime.UtcNow - entries.Key.InteractionStartDateTime).Days,
          NumberOfElapsedDays))
          return numberOfPastInteractionsComparer(i + 2, ((HasEventOccurredCondition<T>)this).NumberOfPastInteractions);
        return false;
      }).SelectMany(entries => (IEnumerable<KeyBehaviorCacheEntry>)entries));
    }

    protected abstract IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache);

    protected abstract bool HasEventOccurredInInteraction(IInteractionData interaction);
  }
}