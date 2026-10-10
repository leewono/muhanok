using System;
using System.Globalization;

namespace Muhanok.Domain
{
    // 2024 Adult Compendium code 22240: total-body active game, moderate effort (4 MET).
    // This estimates gross expenditure; camera recognition does not measure metabolism.
    public sealed class WorkoutEstimate
    {
        public const double AssumedMet=4;
        public double? WeightKg {get; private set;}
        public double ActiveSeconds {get; private set;}
        public double Met {get;set;}=AssumedMet;
        public double EstimatedKcal=>WeightKg.HasValue?Met*3.5*WeightKg.Value/200*ActiveSeconds/60:0;
        public bool SetWeight(string value)
        {
            if(string.IsNullOrWhiteSpace(value)){WeightKg=null; Reset(); return true;}
            if(!double.TryParse(value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out double kg)
                ||double.IsNaN(kg)||double.IsInfinity(kg)||kg<10||kg>350) return false;
            WeightKg=kg; Reset(); return true;
        }
        public void Tick(double seconds,bool playing,bool poseConnected,MotionCommand recognizedMotion)
        {
            if(!WeightKg.HasValue||!playing||!poseConnected||recognizedMotion==MotionCommand.Center
                ||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0) return;
            ActiveSeconds+=seconds;
        }
        // Camera styles count seconds in which the classifier saw real exercise (including running in place).
        public void Tick(double seconds,bool playing,bool active)
        {
            if(!WeightKg.HasValue||!playing||!active||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0) return;
            ActiveSeconds+=seconds;
        }
        public void Reset(){ActiveSeconds=0;}
    }
}
