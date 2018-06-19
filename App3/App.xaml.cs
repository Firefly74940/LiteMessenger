using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using App3.Data;
using App3.ViewModels;
using MetroLog;
using MetroLog.Targets;


namespace App3
{
    /// <summary>
    /// Fournit un comportement spécifique à l'application afin de compléter la classe Application par défaut.
    /// </summary>
    sealed partial class App : Application
    {
        
        private ILogger log;
        

        /// <summary>
        /// Initialise l'objet d'application de singleton.  Il s'agit de la première ligne du code créé
        /// à être exécutée. Elle correspond donc à l'équivalent logique de main() ou WinMain().
        /// </summary>
        public App()
        {
#if DEBUG
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new StreamingFileTarget());
#else
LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Error, LogLevel.Fatal, new FileStreamingTarget());
#endif
            log = LogManagerFactory.DefaultLogManager.GetLogger<App>();
            GlobalCrashHandler.Configure();
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs e)
        {
            this.log.Trace("This is a trace message. On Activated");
            if (e.Kind == ActivationKind.Protocol)
            {
                var args = e as ProtocolActivatedEventArgs;
                // Display the result of the protocol activation if we got here as a result of being activated for a protocol.

                ActivateFromURI(args);
            }

            if (e.Kind == ActivationKind.ContactPanel)
            {
                var args = e as ContactPanelActivatedEventArgs;

                ActivateForContactPanel(args);
            }
        }

        private async void ActivateFromURI(ProtocolActivatedEventArgs args)
        {
            // Parse the URI to extract the protocol and the contact ids
            Uri uri = args.Uri;

            string real = Uri.UnescapeDataString(uri.Query);
            var userID = real.Replace("?ContactRemoteIds=", "");
            if (uri.Scheme == "ms-ipmessaging")
            {
                Frame rootFrame = Window.Current.Content as Frame;

                // Ne répétez pas l'initialisation de l'application lorsque la fenêtre comporte déjà du contenu,
                // assurez-vous juste que la fenêtre est active
                if (rootFrame == null)
                {
                    rootFrame = InitFrameLoginAndUserAgentAndBack(args);
                }

                // Quand la pile de navigation n'est pas restaurée, accédez à la première page,
                // puis configurez la nouvelle page en transmettant les informations requises en tant que
                // paramètre
                if (!DataSource.IsLogedIn)
                    rootFrame.Navigate(typeof(MainPage));
                else
                {
                    var name = await DataSource.GetNameFromRemotId(userID);
                    var header = new ChatHeader()
                    {
                        Href = userID,
                        IsGroup = false,
                        Name = name,
                        UnreadCount = 2
                    };
                    rootFrame.Navigate(typeof(ChatPage), new ChatViewModel(header));
                }

                // Vérifiez que la fenêtre actuelle est active
                Window.Current.Activate();
            }
        }

      
        async void ActivateForContactPanel(ContactPanelActivatedEventArgs e)
        {


            string remoteId = await DataSource.GetRemoteIdForContactIdAsync(e.Contact);

            if (string.IsNullOrEmpty(remoteId)) return;
            {

                Frame rootFrame = Window.Current.Content as Frame;

                // Ne répétez pas l'initialisation de l'application lorsque la fenêtre comporte déjà du contenu,
                // assurez-vous juste que la fenêtre est active
                if (rootFrame == null)
                {
                    rootFrame = InitFrameLoginAndUserAgentAndBack(e);
                }

                if (!DataSource.IsLogedIn)
                    rootFrame.Navigate(typeof(MainPage));
                else
                {
                    var header = new ChatHeader()
                    {
                        Href = remoteId,
                        IsGroup = false,
                        Name = e.Contact.Name,
                        UnreadCount = 2
                    };
                    rootFrame.Navigate(typeof(ChatPage), new ChatViewModel(header));

                }

                // Ensure the current window is active
                Window.Current.Activate();
            }

        }


        /// <summary>
        /// Invoqué lorsque l'application est lancée normalement par l'utilisateur final.  D'autres points d'entrée
        /// seront utilisés par exemple au moment du lancement de l'application pour l'ouverture d'un fichier spécifique.
        /// </summary>
        /// <param name="e">Détails concernant la requête et le processus de lancement.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            this.log.Trace("This is a trace message. On Lunched");
            Frame rootFrame = Window.Current.Content as Frame;

            // Ne répétez pas l'initialisation de l'application lorsque la fenêtre comporte déjà du contenu,
            // assurez-vous juste que la fenêtre est active
            if (rootFrame == null)
            {
                rootFrame = InitFrameLoginAndUserAgentAndBack(e);
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Quand la pile de navigation n'est pas restaurée, accédez à la première page,
                    // puis configurez la nouvelle page en transmettant les informations requises en tant que
                    // paramètre
                    if (!DataSource.IsLogedIn)
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    else
                        rootFrame.Navigate(typeof(ChatList), e.Arguments);
                }
                else
                {
                    if (rootFrame.Content is ChatList)
                    {
                        var chat = rootFrame.Content as ChatList;
                       AccountViewModel.Instance.RefreshChatList();
                    }
                    else
                    {
                        if (!DataSource.IsLogedIn)
                            rootFrame.Navigate(typeof(MainPage), e.Arguments);
                        else
                            rootFrame.Navigate(typeof(ChatList), e.Arguments);
                    }
                }

                // Vérifiez que la fenêtre actuelle est active
                Window.Current.Activate();
            }
        }

        private Frame InitFrameLoginAndUserAgentAndBack(IActivatedEventArgs e)
        {
            this.log.Trace("This is a trace message. On InitFrame");
            Frame rootFrame;
            // Créez un Frame utilisable comme contexte de navigation et naviguez jusqu'à la première page
            rootFrame = new Frame();

            rootFrame.NavigationFailed += OnNavigationFailed;

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                //TODO: chargez l'état de l'application précédemment suspendue
            }

            // Placez le frame dans la fenêtre active
            Window.Current.Content = rootFrame;

            UserAgent.SetUserAgent(DataSource.CustomUserAgent);
            DataSource.InitLocalSettings();
            DataSource.InitSystemHooks();
            SystemNavigationManager.GetForCurrentView().BackRequested +=
                App_BackRequested;
            return rootFrame;
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (rootFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        /// <summary>
        /// Appelé lorsque la navigation vers une page donnée échoue
        /// </summary>
        /// <param name="sender">Frame à l'origine de l'échec de navigation.</param>
        /// <param name="e">Détails relatifs à l'échec de navigation</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Appelé lorsque l'exécution de l'application est suspendue.  L'état de l'application est enregistré
        /// sans savoir si l'application pourra se fermer ou reprendre sans endommager
        /// le contenu de la mémoire.
        /// </summary>
        /// <param name="sender">Source de la requête de suspension.</param>
        /// <param name="e">Détails de la requête de suspension.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: enregistrez l'état de l'application et arrêtez toute activité en arrière-plan
            deferral.Complete();
        }

       
    }
}
