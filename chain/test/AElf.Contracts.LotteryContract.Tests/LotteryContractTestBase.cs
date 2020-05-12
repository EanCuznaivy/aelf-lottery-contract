using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.LotteryContract
{
    // ReSharper disable InconsistentNaming
    public class LotteryContractTestBase : ContractTestBase<LotteryContractTestModule>
    {
        internal LotteryContractContainer.LotteryContractStub LotteryContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }
        private ACS0Container.ACS0Stub ZeroContractStub { get; set; }

        internal ECKeyPair AliceKeyPair { get; set; } = SampleECKeyPairs.KeyPairs.Last();
        internal ECKeyPair BobKeyPair { get; set; } = SampleECKeyPairs.KeyPairs.Reverse().Skip(1).First();
        internal ECKeyPair DefaultKeyPair { get; set; } = SampleECKeyPairs.KeyPairs.First();
        internal static List<ECKeyPair> InitialMinerKeyPairs => SampleECKeyPairs.KeyPairs.Take(5).ToList();

        internal Address AliceAddress => Address.FromPublicKey(AliceKeyPair.PublicKey);
        internal Address BobAddress => Address.FromPublicKey(BobKeyPair.PublicKey);
        internal Address LotteryContractAddress { get; set; }
        internal Address TokenContractAddress { get; set; }
        internal Address ParliamentContractAddress { get; set; }
        internal Address AssociationContractAddress { get; set; }
        internal Address ConsensusContractAddress { get; set; }
        internal Address ProfitContractAddress { get; set; }

        protected LotteryContractTestBase()
        {
            InitializeContracts();
        }

        private void InitializeContracts()
        {
            ZeroContractStub = GetZeroContractStub(DefaultKeyPair);
            
            ParliamentContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentContract).Assembly.Location)),
                        Name = ParliamentSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateParliamentContractMethodCallList()
                    })).Output;
            ParliamentContractStub = GetParliamentContractStub(DefaultKeyPair);

            TokenContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GetTokenContractMethodCallList()
                    })).Output;
            TokenContractStub = GetTokenContractStub(DefaultKeyPair);

            ConsensusContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code =
                            ByteString.CopyFrom(File.ReadAllBytes(typeof(AEDPoSContract).Assembly.Location)),
                        Name = ConsensusSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GetConsensusContractMethodCallList()
                    })).Output;

            LotteryContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code =
                            ByteString.CopyFrom(File.ReadAllBytes(typeof(LotteryContract).Assembly.Location)),
                        Name = HashHelper.ComputeFrom("AElf.ContractNames.LotteryContract"),
                        TransactionMethodCallList =
                            new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                    })).Output;
            LotteryContractStub = GetLotteryContractStub(DefaultKeyPair);
        }

        private ACS0Container.ACS0Stub GetZeroContractStub(ECKeyPair keyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        internal LotteryContractContainer.LotteryContractStub GetLotteryContractStub(ECKeyPair keyPair)
        {
            return GetTester<LotteryContractContainer.LotteryContractStub>(LotteryContractAddress, keyPair);
        }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractStub(ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetConsensusContractStub(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(TokenContractAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GetTokenContractMethodCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            {
                Value =
                {
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(TokenContract.Create),
                        Params = new CreateInput
                        {
                            Symbol = "ELF",
                            Decimals = 8,
                            Issuer = ContractZeroAddress,
                            IsBurnable = true,
                            IsProfitable = true,
                            TokenName = "Elf token",
                            TotalSupply = 10_0000_0000_00000000
                        }.ToByteString()
                    },
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(TokenContract.Issue),
                        Params = new IssueInput
                        {
                            Symbol = "ELF",
                            To = AliceAddress,
                            Amount = 10_0000_0000_00000000
                        }.ToByteString()
                    }
                }
            };
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GetConsensusContractMethodCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            {
                Value =
                {
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(AEDPoSContract.InitialAElfConsensusContract),
                        Params = new InitialAElfConsensusContractInput
                        {
                            PeriodSeconds = 604800L,
                            MinerIncreaseInterval = 31536000
                        }.ToByteString()
                    },
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(AEDPoSContract.FirstRound),
                        Params = new MinerList
                        {
                            Pubkeys = {InitialMinerKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                        }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()).ToByteString()
                    }
                }
            };
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentContractMethodCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            {
                Value =
                {
                    new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                    {
                        MethodName = nameof(ParliamentContract.Initialize),
                        Params = new Parliament.InitializeInput().ToByteString()
                    }
                }
            };
        }
    }
}