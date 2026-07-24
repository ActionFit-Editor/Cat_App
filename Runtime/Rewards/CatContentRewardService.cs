using System;
using System.Collections.Generic;
using System.Globalization;
using ActionFit.Content;

namespace ActionFit.Cat.App
{
    public enum CatRewardKind
    {
        Gold,
        Energy,
        Dia,
        Star,
        BoardItem,
        BoostEnergy,
        UnlimitedEnergy,
        Exp,
        Pass,
        Wave,
        Profile,
        Frame,
    }

    public sealed class CatContentReward
    {
        public CatContentReward(string rewardId, CatRewardKind kind, string itemKey, int amount)
        {
            RewardId = rewardId ?? throw new ArgumentNullException(nameof(rewardId));
            Kind = kind;
            ItemKey = itemKey ?? string.Empty;
            Amount = amount;
        }

        public string RewardId { get; }
        public CatRewardKind Kind { get; }
        public string ItemKey { get; }
        public int Amount { get; }
    }

    public static class CatContentRewardMapper
    {
        public static bool TryCreate(ContentReward reward, out CatContentReward mapped)
        {
            mapped = null;
            if (reward == null || reward.Amount <= 0 || reward.Amount > int.MaxValue)
            {
                return false;
            }

            string rewardId = reward.RewardId;
            int separatorIndex = rewardId.IndexOf('/');
            string kindName = separatorIndex < 0 ? rewardId : rewardId.Substring(0, separatorIndex);
            string itemKey = separatorIndex < 0 ? string.Empty : rewardId.Substring(separatorIndex + 1);
            if (!Enum.TryParse(kindName, true, out CatRewardKind kind)
                || !Enum.IsDefined(typeof(CatRewardKind), kind)
                || !IsItemKeyValid(kind, itemKey))
            {
                return false;
            }

            string canonicalRewardId = string.IsNullOrEmpty(itemKey)
                ? kind.ToString()
                : $"{kind}/{itemKey}";
            mapped = new CatContentReward(canonicalRewardId, kind, itemKey, (int)reward.Amount);
            return true;
        }

        private static bool IsItemKeyValid(CatRewardKind kind, string itemKey)
        {
            if (kind == CatRewardKind.BoardItem)
            {
                string[] values = itemKey.Split('_');
                return values.Length == 2
                    && int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out int group)
                    && int.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                    && group > 0
                    && value > 0;
            }

            if (kind == CatRewardKind.Pass)
            {
                return int.TryParse(itemKey, NumberStyles.None, CultureInfo.InvariantCulture, out int passIndex)
                    && passIndex > 0;
            }

            if (kind == CatRewardKind.Profile || kind == CatRewardKind.Frame)
            {
                return !string.IsNullOrWhiteSpace(itemKey);
            }

            return string.IsNullOrEmpty(itemKey);
        }
    }

    public interface ICatContentRewardPersistence
    {
        CatRewardLedger Load();
        void SaveAndFlush(CatRewardLedger ledger);
    }

    public sealed class CatRewardLedger
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, CatRewardTransaction> Transactions { get; set; } =
            new Dictionary<string, CatRewardTransaction>(StringComparer.Ordinal);
    }

    public sealed class CatRewardTransaction
    {
        public int Status { get; set; }
        public List<CatRewardReceipt> Rewards { get; set; } = new List<CatRewardReceipt>();
    }

    public sealed class CatRewardReceipt
    {
        public string RewardId { get; set; }
        public int Amount { get; set; }
        public bool Granted { get; set; }
    }

    public sealed class CatContentRewardService : IContentRewardService
    {
        public const int CurrentSchemaVersion = 1;
        public const int PendingStatus = 1;
        public const int ConfirmedStatus = 2;

        private readonly object _sync = new object();
        private readonly ICatContentRewardPersistence _persistence;
        private readonly Action<CatContentReward> _grantReward;
        private readonly Func<bool> _isAvailable;

        public CatContentRewardService(
            ICatContentRewardPersistence persistence,
            Action<CatContentReward> grantReward,
            Func<bool> isAvailable = null)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _grantReward = grantReward ?? throw new ArgumentNullException(nameof(grantReward));
            _isAvailable = isAvailable ?? (() => true);
        }

        public bool IsAvailable => _isAvailable();

        public bool HasGranted(string transactionId)
        {
            transactionId = ValidateIdentifier(transactionId, nameof(transactionId));
            lock (_sync)
            {
                CatRewardLedger ledger = LoadAndValidateLedger();
                return ledger.Transactions.TryGetValue(transactionId, out CatRewardTransaction transaction)
                    && transaction.Status == ConfirmedStatus;
            }
        }

        public bool GrantOnce(string transactionId, IReadOnlyList<ContentReward> rewards)
        {
            transactionId = ValidateIdentifier(transactionId, nameof(transactionId));
            List<CatContentReward> normalizedRewards = ValidateAndNormalizeRewards(rewards);
            if (!IsAvailable)
            {
                throw new InvalidOperationException("The Cat Merge reward runtime is not ready.");
            }

            lock (_sync)
            {
                CatRewardLedger ledger = LoadAndValidateLedger();
                if (!ledger.Transactions.TryGetValue(transactionId, out CatRewardTransaction transaction))
                {
                    transaction = CreatePendingTransaction(normalizedRewards);
                    ledger.Transactions.Add(transactionId, transaction);
                    _persistence.SaveAndFlush(ledger);
                }
                else
                {
                    ValidateTransaction(transaction);
                    ValidateRewardSnapshot(transaction, normalizedRewards);
                    if (transaction.Status == ConfirmedStatus)
                    {
                        return false;
                    }
                }

                for (int index = 0; index < normalizedRewards.Count; index++)
                {
                    CatRewardReceipt receipt = transaction.Rewards[index];
                    if (receipt.Granted)
                    {
                        continue;
                    }

                    _grantReward(normalizedRewards[index]);
                    receipt.Granted = true;
                    _persistence.SaveAndFlush(ledger);
                }

                VerifyEveryRewardGranted(transaction);
                transaction.Status = ConfirmedStatus;
                _persistence.SaveAndFlush(ledger);

                CatRewardLedger verifiedLedger = LoadAndValidateLedger();
                if (!verifiedLedger.Transactions.TryGetValue(
                        transactionId,
                        out CatRewardTransaction verifiedTransaction))
                {
                    throw new InvalidOperationException("The Cat Merge reward transaction receipt was not persisted.");
                }

                ValidateTransaction(verifiedTransaction);
                ValidateRewardSnapshot(verifiedTransaction, normalizedRewards);
                if (verifiedTransaction.Status != ConfirmedStatus)
                {
                    throw new InvalidOperationException("The Cat Merge reward transaction was not confirmed.");
                }

                return true;
            }
        }

        private CatRewardLedger LoadAndValidateLedger()
        {
            CatRewardLedger ledger = _persistence.Load();
            if (ledger == null)
            {
                return new CatRewardLedger();
            }

            if (ledger.SchemaVersion != CurrentSchemaVersion || ledger.Transactions == null)
            {
                throw new InvalidOperationException("The Cat Merge content reward ledger is corrupted.");
            }

            foreach (KeyValuePair<string, CatRewardTransaction> pair in ledger.Transactions)
            {
                ValidateIdentifier(pair.Key, nameof(ledger.Transactions));
                ValidateTransaction(pair.Value);
            }

            return ledger;
        }

        private static CatRewardTransaction CreatePendingTransaction(
            IReadOnlyList<CatContentReward> rewards)
        {
            var transaction = new CatRewardTransaction
            {
                Status = PendingStatus,
            };
            for (int index = 0; index < rewards.Count; index++)
            {
                CatContentReward reward = rewards[index];
                transaction.Rewards.Add(new CatRewardReceipt
                {
                    RewardId = reward.RewardId,
                    Amount = reward.Amount,
                    Granted = false,
                });
            }

            return transaction;
        }

        private static void ValidateTransaction(CatRewardTransaction transaction)
        {
            if (transaction == null
                || (transaction.Status != PendingStatus && transaction.Status != ConfirmedStatus)
                || transaction.Rewards == null
                || transaction.Rewards.Count == 0)
            {
                throw new InvalidOperationException("The Cat Merge content reward ledger contains an invalid transaction.");
            }

            var rewardIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < transaction.Rewards.Count; index++)
            {
                CatRewardReceipt receipt = transaction.Rewards[index];
                if (receipt == null
                    || string.IsNullOrWhiteSpace(receipt.RewardId)
                    || receipt.Amount <= 0
                    || !rewardIds.Add(receipt.RewardId)
                    || !CatContentRewardMapper.TryCreate(
                        new ContentReward(receipt.RewardId, receipt.Amount),
                        out _)
                    || (transaction.Status == ConfirmedStatus && !receipt.Granted))
                {
                    throw new InvalidOperationException("The Cat Merge content reward ledger contains an invalid reward.");
                }
            }
        }

        private static void ValidateRewardSnapshot(
            CatRewardTransaction transaction,
            IReadOnlyList<CatContentReward> rewards)
        {
            if (transaction.Rewards.Count != rewards.Count)
            {
                throw new InvalidOperationException("The Cat Merge reward snapshot changed for an existing transaction.");
            }

            for (int index = 0; index < rewards.Count; index++)
            {
                CatRewardReceipt receipt = transaction.Rewards[index];
                CatContentReward reward = rewards[index];
                if (!string.Equals(receipt.RewardId, reward.RewardId, StringComparison.Ordinal)
                    || receipt.Amount != reward.Amount)
                {
                    throw new InvalidOperationException("The Cat Merge reward snapshot changed for an existing transaction.");
                }
            }
        }

        private static void VerifyEveryRewardGranted(CatRewardTransaction transaction)
        {
            for (int index = 0; index < transaction.Rewards.Count; index++)
            {
                if (!transaction.Rewards[index].Granted)
                {
                    throw new InvalidOperationException("The Cat Merge reward transaction has an incomplete grant journal.");
                }
            }
        }

        private static List<CatContentReward> ValidateAndNormalizeRewards(
            IReadOnlyList<ContentReward> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                throw new ArgumentException("Rewards must not be empty.", nameof(rewards));
            }

            var totals = new Dictionary<string, CatContentReward>(StringComparer.Ordinal);
            for (int index = 0; index < rewards.Count; index++)
            {
                if (!CatContentRewardMapper.TryCreate(rewards[index], out CatContentReward reward))
                {
                    throw new ArgumentException("Rewards contain an unsupported Cat Merge reward.", nameof(rewards));
                }

                if (totals.TryGetValue(reward.RewardId, out CatContentReward existing))
                {
                    int amount = checked(existing.Amount + reward.Amount);
                    totals[reward.RewardId] = new CatContentReward(
                        reward.RewardId,
                        reward.Kind,
                        reward.ItemKey,
                        amount);
                }
                else
                {
                    totals.Add(reward.RewardId, reward);
                }
            }

            var normalized = new List<CatContentReward>(totals.Values);
            normalized.Sort((left, right) => string.CompareOrdinal(left.RewardId, right.RewardId));
            return normalized;
        }

        private static string ValidateIdentifier(string value, string parameterName)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value must not be empty or whitespace.", parameterName)
                : value;
        }
    }
}
