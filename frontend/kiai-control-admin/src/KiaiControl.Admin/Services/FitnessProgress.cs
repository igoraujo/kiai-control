using KiaiControl.Contracts.Fitness;

namespace KiaiControl.Admin.Services;
public static class FitnessProgress
{
    public static double Percentage(FitnessGoalResponse goal)
    {
        if (goal.Direction == "decrease")
            return goal.InitialValue <= goal.TargetValue ? 0 : (double)Math.Clamp((goal.InitialValue - goal.CurrentValue) / (goal.InitialValue - goal.TargetValue) * 100, 0, 100);
        return goal.TargetValue <= 0 ? 0 : (double)Math.Clamp(goal.CurrentValue / goal.TargetValue * 100, 0, 100);
    }
}
