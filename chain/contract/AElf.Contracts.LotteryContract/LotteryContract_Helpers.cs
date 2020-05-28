using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Profit;
using AElf.CSharp.Core;
using AElf.Types;

namespace AElf.Contracts.LotteryContract
{
    public partial class LotteryContract
    {
        private void AssertSenderIsAdmin()
        {
            Assert(Context.Sender == State.Admin.Value, "Sender should be admin.");
        }

        private void CreateFinalRewardProfitScheme()
        {
            State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
            {
                Token = Context.TransactionId,
                Manager = Context.Self,
                IsReleaseAllBalanceEveryTimeByDefault = true,
                CanRemoveBeneficiaryDirectly = true
            });
            State.FinalRewardProfitSchemeId.Value =
                Context.GenerateId(State.ProfitContract.Value, Context.TransactionId);
        }

        private void InitialNextPeriod()
        {
            var periodBody = State.Periods[State.CurrentPeriod.Value];
            if (periodBody == null)
            {
                periodBody = new PeriodBody
                {
                    StartId = State.SelfIncreasingIdForLottery.Value,
                    BlockNumber = Context.CurrentHeight.Add(State.DrawingLag.Value),
                    RandomHash = Hash.Empty
                };
            }
            else
            {
                periodBody.StartId = State.SelfIncreasingIdForLottery.Value;
                periodBody.BlockNumber = Context.CurrentHeight.Add(State.DrawingLag.Value);
                periodBody.RandomHash = Hash.Empty;
            }

            State.Periods[State.CurrentPeriod.Value] = periodBody;
        }

        private void DealWithLotteries(List<int> levelsCount, Hash randomHash)
        {
            var currentPeriodNumber = State.CurrentPeriod.Value;
            var previousPeriodNumber = currentPeriodNumber.Sub(1);
            var poolCount = State.Periods[currentPeriodNumber].StartId.Sub(1);

            var period = State.Periods[previousPeriodNumber];
            if (randomHash == null)
            {
                // Only can happen in test cases.
                randomHash = HashHelper.ComputeFrom(Context.PreviousBlockHash);
            }

            period.RandomHash = randomHash;

            var rewardCount = levelsCount.Sum();
            State.RewardCount.Value = State.RewardCount.Value.Add(rewardCount);
            Assert(rewardCount > 0, "Reward pool cannot be empty.");
            Assert(poolCount >= State.RewardCount.Value,
                $"Too many rewards, lottery pool size: {poolCount.Sub(State.RewardCount.Value)}.");

            var ranks = new List<int>();

            for (var i = 0; i < levelsCount.Count; i++)
            {
                for (var j = 0; j < levelsCount[i]; j++)
                {
                    ranks.Add(i.Add(1));
                }
            }

            var rewardIds = new List<long>();
            var rewardId = Math.Abs(randomHash.ToInt64() % poolCount).Add(1);

            for (var i = 0; i < rewardCount; i++)
            {
                while (State.Lotteries[rewardId].Level > 0)
                {
                    // Keep updating luckyIndex
                    randomHash = HashHelper.ComputeFrom(randomHash);
                    rewardId = Math.Abs(randomHash.ToInt64() % poolCount).Add(1);
                }

                rewardIds.Add(rewardId);
                State.Lotteries[rewardId].Level = ranks[i];
            }

            period.RewardIds.Add(rewardIds);
            State.Periods[previousPeriodNumber] = period;
        }
    }
}