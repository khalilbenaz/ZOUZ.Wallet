namespace ZOUZ.Wallet.Core.Entities.Enum;

public enum KycLevel
{
    None,
    Basic,    // Vérification basique (téléphone, email)
    Standard, // Vérification CIN
    Advanced  // Vérification complète
}