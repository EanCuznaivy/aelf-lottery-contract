using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.LotteryContract;
using AElf.OS.Node.Application;
using AElf.Types;
using InitializeInput = AElf.Contracts.LotteryContract.InitializeInput;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForLottery()
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("Lottery")).Value,
                Hash.FromString("AElf.ContractNames.Lottery"), GenerateLotteryInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateLotteryInitializationCallList()
        {
            var LotteryContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            LotteryContractMethodCallList.Add(
                nameof(LotteryContractContainer.LotteryContractStub.Initialize),
                new InitializeInput
                {
                    TokenSymbol = _economicOptions.TokenName
                });
            return LotteryContractMethodCallList;
        }
    }
}