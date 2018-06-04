using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Analytics;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.Rules;

namespace Sitecore.Support.Analytics.Rules.Conditions
{
  public class GoalWasTriggeredDuringPastOrCurrentInteractionCondition<T> : HasEventOccurredCondition<T>
    where T : RuleContext
  {
    private Guid? goalGuid;
    private bool goalGuidInitialized;

    public GoalWasTriggeredDuringPastOrCurrentInteractionCondition()
      : base(false)
    {
    }

    protected GoalWasTriggeredDuringPastOrCurrentInteractionCondition(bool filterByCustomData)
      : base(filterByCustomData)
    {
    }

    public string GoalId { get; set; }

    private Guid? GoalGuid
    {
      get
      {
        if (goalGuidInitialized)
          return goalGuid;
        try
        {
          goalGuid = new Guid(GoalId);
        }
        catch
        {
          Log.Warn($"Could not convert value to guid: {GoalId}", GetType());
        }
        goalGuidInitialized = true;
        return goalGuid;
      }
    }

    protected override bool Execute(T ruleContext)
    {
      Assert.ArgumentNotNull(ruleContext, "ruleContext");
      Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
      Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");
      Assert.IsNotNull(Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
      if (!GoalGuid.HasValue)
        return false;
      if (HasEventOccurredInInteraction(Tracker.Current.Session.Interaction))
        return true;
      Assert.IsNotNull(Tracker.Current.Contact, "Tracker.Current.Contact is not initialized");
      return FilterKeyBehaviorCacheEntries(Tracker.Current.Contact.GetKeyBehaviorCache()).Any(entry =>
      {
        var id = entry.Id;
        var goalGuid = GoalGuid;
        if (!goalGuid.HasValue)
          return false;
        return id == goalGuid.GetValueOrDefault();
      });
    }

    protected override IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache)
    {
      Assert.ArgumentNotNull(keyBehaviorCache, "keyBehaviorCache");
      return keyBehaviorCache.Goals;
    }

    protected override bool HasEventOccurredInInteraction(IInteractionData interaction)
    {
      Assert.ArgumentNotNull(interaction, "interaction");
      Assert.IsNotNull(interaction.Pages, "interaction.Pages is not initialized.");
      return interaction.Pages.SelectMany(page => page.PageEvents).Any(pageEvent =>
      {
        if (!pageEvent.IsGoal)
          return false;
        var eventDefinitionId = pageEvent.PageEventDefinitionId;
        var goalGuid = GoalGuid;
        if (!goalGuid.HasValue)
          return false;
        return eventDefinitionId == goalGuid.GetValueOrDefault();
      });
    }
  }
}