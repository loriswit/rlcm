using System.Linq;

namespace Rlcm.Game
{
    public class Challenge
    {
        private readonly Memory _memory;

        private int _challengeAddress;
        private int _seedAddress;
        private string _isg;

        private static readonly int[] ChallengeOffsets = {0xae7868};
        private static readonly int[] SeedOffsets = {0xae483C, 0xc};

        private const int OnlineOffset = 0x268;
        private const int ChallengeSeedOffset = 0x294;
        private const int TypeOffset = 0x298;
        private const int GoalOffset = 0x2a0;
        private const int LimitOffset = 0x2a4;
        private const int IsgOffset = 0x2c8;

        private const int GeneratorSeedOffset = 0x714;
        private const int SeedDojoOffset = 0x248;

        public static class Type
        {
            public const int AsFarAsYouCan = 0;
            public const int GrabThemQuickly = 1;
            public const int GetThereQuickly = 2;
            public const int AgainstTheClock = 3;
            public const int AsManyAsYouCan = 4;
            public const int GrabThereQuickly2 = 5;
        }

        public Challenge(Memory memory)
        {
            _memory = memory;
        }

        public bool Load()
        {
            // check if the game is running
            if (!_memory.Load())
                return false;

            _challengeAddress = _memory.GetAddress(ChallengeOffsets);
            _seedAddress = _memory.GetAddress(SeedOffsets);

            _isg = _memory.ReadString(_challengeAddress + IsgOffset);
            return _isg.StartsWith("challenge_");
        }

        public string GetLevel()
        {
            return _isg.Split('_')[1];
        }

        public string GetDifficulty()
        {
            return _isg.Split('_').Last().Split('.').First();
        }

        public bool IsOnline()
        {
            return _memory.ReadInteger(_challengeAddress + OnlineOffset) > 0;
        }

        // Challenge types:
        // 0 => As far as you can
        // 1 => Grab them quickly
        // 2 => Get there quickly
        // 3 => As far as you can (time limit)
        // 4 => As many as you can
        // 5 => As many as you can (time limit)

        public int GetChallengeType()
        {
            return _memory.ReadInteger(_challengeAddress + TypeOffset);
        }

        public void SetChallengeType(int type)
        {
            _memory.WriteInteger(_challengeAddress + TypeOffset, type);
        }

        public float GetGoal()
        {
            return _memory.ReadFloat(_challengeAddress + GoalOffset);
        }

        public void SetGoal(float goal)
        {
            _memory.WriteFloat(_challengeAddress + GoalOffset, goal);
        }

        public float GetLimit()
        {
            return _memory.ReadFloat(_challengeAddress + LimitOffset);
        }

        public void SetLimit(float limit)
        {
            _memory.WriteFloat(_challengeAddress + LimitOffset, limit);
        }

        public int GetGeneratorSeed()
        {
            var offset = GetLevel() == "shaolin" ? SeedDojoOffset : GeneratorSeedOffset;
            return _memory.ReadInteger(_seedAddress + offset);
        }

        public int GetChallengeSeed()
        {
            return _memory.ReadInteger(_challengeAddress + ChallengeSeedOffset);
        }

        public void SetSeed(int seed)
        {
            var offset = GetLevel() == "shaolin" ? SeedDojoOffset : GeneratorSeedOffset;
            _memory.WriteInteger(_seedAddress + offset, seed);
            _memory.WriteInteger(_challengeAddress + ChallengeSeedOffset, seed);
        }
    }
}
