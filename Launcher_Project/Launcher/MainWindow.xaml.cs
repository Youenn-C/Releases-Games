using System.IO.Compression;
using System.IO;
using System.Net;
using System.Windows;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckForUpdates(null, null);
        }

        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            if (await GitHubUpdater.IsUpdateAvailable())
            {
                UpdateButton.Visibility = Visibility.Visible;
                LaunchButton.IsEnabled = false;
                MessageBox.Show("Mise à jour disponible !");
            }
            else
            {
                MessageBox.Show("Le jeu est à jour !");
            }
        }

        private async void StartUpdate(object sender, RoutedEventArgs e)
        {
            await GameUpdater.UpdateGame();
            UpdateButton.Visibility = Visibility.Hidden;
            LaunchButton.IsEnabled = true;
        }

        private void LaunchGame(object sender, RoutedEventArgs e)
        {
            string gamePath = "C:\\Users\\youen\\Desktop\\Test_Launcher\\Sapon_A_Soap_Story"; // Remplace avec le vrai chemin

            if (File.Exists(gamePath))
            {
                Process.Start(gamePath);
            }
            else
            {
                MessageBox.Show("Le jeu n'est pas installé ou le fichier est manquant !");
            }
        }
    }
}

public class GameUpdater
{
    private static readonly string downloadUrl = "https://github.com/Youenn-C/Releases-Games/releases/latest/download/sapon_a_soap_story.zip";
    private static readonly string extractPath = "C:\\Users\\youen\\Desktop\\Test_Launcher\\Sapon_A_Soap_Story";
    private static readonly string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sapon_a_soap_story.zip");


    public static async Task UpdateGame()
    {
        try
        {
            // Vérifier si un fichier ZIP corrompu existe et le supprimer
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // Téléchargement sécurisé du fichier ZIP
            using (WebClient client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(downloadUrl), zipPath);
                client.Dispose(); // S'assurer que la connexion est bien fermée
            }

            long fileSize = new FileInfo(zipPath).Length;
            if (fileSize < 100) // Vérifie si le fichier est trop petit (100 octets est une estimation)
            {
                MessageBox.Show($"Le fichier ZIP semble corrompu (taille : {fileSize} octets). Retéléchargement en cours ...");
                File.Delete(zipPath);
                return;
            }

            // Vérifier si le fichier ZIP a été téléchargé correctement
            if (!File.Exists(zipPath) || new FileInfo(zipPath).Length == 0)
            {
                MessageBox.Show("Le fichier ZIP est vide ou corrompu. Veuillez réessayer.");
                return;
            }

            // Vérifier si le fichier ZIP est valide avant d'extraire
            if (!IsZipValid(zipPath))
            {
                MessageBox.Show("Le fichier ZIP est corrompu ou invalide.");
                File.Delete(zipPath); // Supprime le fichier corrompu
                return;
            }

            // Extraire le fichier ZIP
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
            MessageBox.Show("Mise à jour terminée avec succès !");

            using (ZipArchive zip = ZipFile.OpenRead(zipPath))
            {
                if (zip.Entries.Count == 0)
                {
                    MessageBox.Show("Erreur : Le fichier ZIP est vide !");
                    return;
                }
            }

            // Extraction sécurisée du fichier ZIP
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la mise à jour : {ex.Message}");
        }
    }

    // Vérifie si un fichier ZIP est valide avant de l'extraire
    private static bool IsZipValid(string zipFilePath)
    {
        try
        {
            using (ZipArchive zip = ZipFile.OpenRead(zipFilePath))
            {
                return zip.Entries.Count > 0; // Vérifie si le ZIP contient bien des fichiers
            }
        }
        catch
        {
            return false; // ZIP invalide ou corrompu
        }
    }
}


public class GitHubUpdater
{
    private static readonly string hashUrl = "https://raw.githubusercontent.com/Youenn-C/Releases-Games/refs/heads/main/Releases-Games/Sapon_A_Soap_Story/sapon_a_soap_story_hash.txt";
    private static readonly string localZipPath = "C:\\Users\\youen\\Desktop\\Test_Launcher\\Sapon_A_Soap_Story";

    public static async Task<bool> IsUpdateAvailable()
    {
        using (HttpClient client = new HttpClient())
        {
            // Récupérer le hash du fichier distant (hash.txt)
            string onlineHash = await client.GetStringAsync(hashUrl);
            onlineHash = onlineHash.Trim();

            // Calculer le hash du fichier local s'il existe
            string localHash = File.Exists(localZipPath) ? ComputeSHA256(localZipPath) : string.Empty;

            // Comparer les hash
            return !onlineHash.Equals(localHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ComputeSHA256(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;

        using (SHA256 sha256 = SHA256.Create())
        using (FileStream stream = File.OpenRead(filePath))
        {
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}


