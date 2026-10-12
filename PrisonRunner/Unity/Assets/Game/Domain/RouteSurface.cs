using System;

namespace Muhanok.Domain
{
    // Logical lane/jump coordinates stay unchanged. These offsets place the continuous world surface.
    public static class RouteSurface
    {
        public const float Length=24f;
        private static readonly int[] route=RoutePlan.AuthoredLap;
        // The active run's plan. Without one, the authored lap repeats (editor tools and tests).
        private static RoutePlan plan;
        public static void UsePlan(RoutePlan runPlan)=>plan=runPlan;
        public static int SequenceLength=>route.Length;
        public static int TypeAt(int sequence)=>plan!=null?plan.TypeAt(sequence):route[Math.Max(0,sequence)%route.Length];
        public static int CycleAt(int sequence)=>plan!=null?plan.CycleAt(sequence):Math.Max(0,sequence)/route.Length;
        public static int SequenceAt(float z)=>(int)Math.Floor(Math.Max(0,z)/Length);
        private static bool CartType(int type)=>type==1||type==2;
        public static bool IsCart(float z)=>CartType(TypeAt(SequenceAt(z)));
        public static float CartProgress(float z)
        {
            int sequence=SequenceAt(z);
            if(!CartType(TypeAt(sequence)))return 0;
            int start=sequence,end=sequence;
            while(start>0&&CartType(TypeAt(start-1)))start--;
            while(end-start<64&&CartType(TypeAt(end+1)))end++;
            return Math.Max(0,Math.Min(1,(z-start*Length)/((end-start+1)*Length)));
        }
        // Metres until the current sprint (walking) or rest (cart) section changes.
        public static float SegmentRemaining(float z)
        {
            int sequence=SequenceAt(z),end=sequence; bool cart=CartType(TypeAt(sequence));
            while(end-sequence<64&&CartType(TypeAt(end+1))==cart)end++;
            return Math.Max(0,(end+1)*Length-z);
        }
        // Negative is a missing left rail, positive is a missing right rail.
        public static int CartMissingSide(int type,float localZ)
        {
            if(type!=1&&type!=2)return 0;
            int row=Math.Abs(localZ-8)<=1.5f?0:Math.Abs(localZ-18)<=1.5f?1:-1;
            return row<0?0:((row+type)%2==0?1:-1);
        }
        public static float EntryHeight(int type)=>type>=4&&type<=6||type>=8&&type<=11?3f:0f;
        public static string AreaAt(float distance)
        {
            int type=TypeAt((int)Math.Floor(Math.Max(0,distance)/Length));
            return type==1||type==2?"광산 수레":type<4?"광산":type==7?"계단":type>=11?"사육장":"감옥 복도";
        }
        public static float Rise(int type)=>type==7?3f:type==11?-3f:0f;
        public static float LocalHeight(int type,float z)
        {
            float t=Math.Max(0,Math.Min(1,z/Length));
            return Rise(type)*t*t*(3-2*t);
        }
        public static float LocalX(int type,float z)
        {
            float amount=type==1?3.2f:type==2?-3.2f:type==11?.7f:type==12?-.7f:0;
            double wave=Math.Sin(Math.PI*Math.Max(0,Math.Min(1,z/Length)));
            return amount*(float)(wave*wave);
        }
        public static void Sample(float distance,out float x,out float y)
        {
            int sequence=Math.Max(0,(int)Math.Floor(distance/Length)); int type=TypeAt(sequence);
            float z=distance-sequence*Length;
            x=LocalX(type,z); y=EntryHeight(type)+LocalHeight(type,z);
        }
    }
}
