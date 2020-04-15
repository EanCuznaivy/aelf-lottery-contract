using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.LotteryContract
{
    // ReSharper disable InconsistentNaming
    public partial class LotteryContractTest : LotteryContractTestBase
    {
        private const long Price = 10_00000000;

        private LotteryContractContainer.LotteryContractStub AliceLotteryContractStub =>
            GetLotteryContractStub(AliceKeyPair);
        private TokenContractContainer.TokenContractStub AliceTokenContractStub => GetTokenContractStub(AliceKeyPair);

        private LotteryContractContainer.LotteryContractStub BobLotteryContractStub => GetLotteryContractStub(BobKeyPair);
        private TokenContractContainer.TokenContractStub BobTokenContractStub => GetTokenContractStub(BobKeyPair);

        private async Task InitialDAOContract()
        {
            await LotteryContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbol = "ELF",
                MaximumAmount = 100,
                Price = Price,
                DrawingLag = 1
            });
        }

        [Fact]
        public async Task LotteryTest()
        {
            await InitialDAOContract();

            await AliceTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = Price * 10
            });
            await AliceLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = 10
            });
            await LotteryContractStub.PrepareDraw.SendAsync(new Empty());
            await LotteryContractStub.Draw.SendAsync(new DrawInput
            {
                LevelsCount = {1, 1, 1}
            });
            var rewardResult = await AliceLotteryContractStub.GetRewardResult.CallAsync(new Int64Value
            {
                Value = 1
            });
            var reward = rewardResult.RewardLotteries.First();
            await AliceLotteryContractStub.TakeReward.SendAsync(new TakeRewardInput
            {
                LotteryId = reward.Id,
                Period = rewardResult.Period,
                RegistrationInformation = "hiahiahia"
            });
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = AliceAddress,
                    Symbol = "ELF"
                });
                balance.Balance.ShouldBePositive();
            }
        }
    }
}