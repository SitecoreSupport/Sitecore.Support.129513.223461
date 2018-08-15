using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;

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
      Assert.ArgumentNotNull((object)keyBehaviorCache, nameof(keyBehaviorCache));
      IEnumerable<KeyBehaviorCacheEntry> behaviorCacheEntries = this.FilterKeyBehaviorCacheEntriesByInteractionConditions(keyBehaviorCache.Campaigns.Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.Channels).Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.CustomValues).Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.Goals).Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.Outcomes).Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.PageEvents).Concat<KeyBehaviorCacheEntry>((IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.Venues));
      if (!this.filterByCustomData)
        return Assert.ResultNotNull<IEnumerable<KeyBehaviorCacheEntry>>(this.GetKeyBehaviorCacheEntries(keyBehaviorCache).Intersect<KeyBehaviorCacheEntry>(behaviorCacheEntries, (IEqualityComparer<KeyBehaviorCacheEntry>)new KeyBehaviorCacheEntry.KeyBehaviorCacheEntryEqualityComparer()));
      if (this.CustomData == null)
      {
        Log.Warn("CustomData can not be null", (object)this.GetType());
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      }
      IEnumerable<KeyBehaviorCacheEntry> second = behaviorCacheEntries.Where<KeyBehaviorCacheEntry>((Func<KeyBehaviorCacheEntry, bool>)(entry =>
      {
        if (entry.Data != null)
          return Sitecore.Support.Rules.Conditions.ConditionsUtility.CompareStrings(entry.Data, this.CustomData, this.CustomDataOperatorId);
        return false;
      }));
      return Assert.ResultNotNull<IEnumerable<KeyBehaviorCacheEntry>>(this.GetKeyBehaviorCacheEntries(keyBehaviorCache).Intersect<KeyBehaviorCacheEntry>(second, (IEqualityComparer<KeyBehaviorCacheEntry>)new KeyBehaviorCacheEntry.KeyBehaviorCacheEntryEqualityComparer()));
    }

    protected virtual IEnumerable<KeyBehaviorCacheEntry> FilterKeyBehaviorCacheEntriesByInteractionConditions(IEnumerable<KeyBehaviorCacheEntry> keyBehaviorCacheEntries)
    {
      Assert.ArgumentNotNull((object)keyBehaviorCacheEntries, nameof(keyBehaviorCacheEntries));
      if (Sitecore.Support.Rules.Conditions.ConditionsUtility.GetInt32Comparer(this.NumberOfElapsedDaysOperatorId) == null)
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      Func<int, int, bool> numberOfPastInteractionsComparer = Sitecore.Support.Rules.Conditions.ConditionsUtility.GetInt32Comparer(this.NumberOfPastInteractionsOperatorId);
      Func<int, int, bool> numberOfElapsedDaysOperatorsComparer = Sitecore.Support.Rules.Conditions.ConditionsUtility.GetInt32Comparer(this.NumberOfElapsedDaysOperatorId);
      if (numberOfPastInteractionsComparer == null)
        return Enumerable.Empty<KeyBehaviorCacheEntry>();
      return Assert.ResultNotNull<IEnumerable<KeyBehaviorCacheEntry>>(Enumerable.SelectMany(Enumerable.Where(Enumerable.OrderByDescending(Enumerable.GroupBy(keyBehaviorCacheEntries, (KeyBehaviorCacheEntry entry) => new
      {
        InteractionId = entry.InteractionId,
        InteractionStartDateTime = entry.InteractionStartDateTime
      }), entries => entries.Key.InteractionStartDateTime), (entries, i) =>
      {
        if (numberOfElapsedDaysOperatorsComparer((DateTime.UtcNow - entries.Key.InteractionStartDateTime).Days, this.NumberOfElapsedDays))
          return numberOfPastInteractionsComparer(i + 2, this.NumberOfPastInteractions);
        return false;
      }), entries => (IEnumerable<KeyBehaviorCacheEntry>)entries));
    }

    protected abstract IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache);

    protected abstract bool HasEventOccurredInInteraction(IInteractionData interaction);
  }
}