namespace Matterless.Floorcraft
{
    /// <summary>
    /// Standard ERC-1155 Multi-Token Contract ABI
    /// Source: https://eips.ethereum.org/EIPS/eip-1155
    /// </summary>
    public static class ERC1155ABI
    {
        public const string JSON = @"[
            {
                ""inputs"": [
                    {
                        ""internalType"": ""address"",
                        ""name"": ""account"",
                        ""type"": ""address""
                    },
                    {
                        ""internalType"": ""uint256"",
                        ""name"": ""id"",
                        ""type"": ""uint256""
                    }
                ],
                ""name"": ""balanceOf"",
                ""outputs"": [
                    {
                        ""internalType"": ""uint256"",
                        ""name"": """",
                        ""type"": ""uint256""
                    }
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    {
                        ""internalType"": ""address[]"",
                        ""name"": ""accounts"",
                        ""type"": ""address[]""
                    },
                    {
                        ""internalType"": ""uint256[]"",
                        ""name"": ""ids"",
                        ""type"": ""uint256[]""
                    }
                ],
                ""name"": ""balanceOfBatch"",
                ""outputs"": [
                    {
                        ""internalType"": ""uint256[]"",
                        ""name"": """",
                        ""type"": ""uint256[]""
                    }
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    {
                        ""internalType"": ""uint256"",
                        ""name"": ""tokenId"",
                        ""type"": ""uint256""
                    }
                ],
                ""name"": ""uri"",
                ""outputs"": [
                    {
                        ""internalType"": ""string"",
                        ""name"": """",
                        ""type"": ""string""
                    }
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            }
        ]";
    }
}

