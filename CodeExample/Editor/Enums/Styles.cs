using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor.Enums
{
    [ExcludeFromCodeCoverage]
    public class Styles
    {
        public enum RenderAs
        {
            Link = 0,
            Button = 1,
        }
        public enum RenderStyle
        {
            ArticleCTA = 0,
            ArticleLink = 1,
        }
        public enum ButtonStyle
        {
            Primary = 0,
            Secondary = 1,
            [Description("Secondary Black Outlined")]
            SecondaryBlackOutlined = 2,
            [Description("Secondary Blue Outlined")]
            SecondaryBlueOutlined = 3,
        }
        public enum ButtonStyleZipCode
        {
            None = 0,
            Secondary = 1,
            Arrow = 2,
            Nina = 3,
            External = 4,
        }
        public enum ZipCodeStyle
        {
            Input = 0,
            Paragraph = 1,
        }
        public enum ButtonStyleArticle
        {
            Primary = 0,
            [Description("Secondary Black Outlined")]
            Secondary = 1,
            Arrow = 2,
            [Description("Secondary Blue Outlined")]
            SecondaryBlueOutlined = 3,
            None = 1000,
        }
        public enum ButtonStyleCompaign
        {
            None = 0,
            Primary = 1,
            Secondary = 2,
        }
        public enum LinkItemType
        {
            Internal = 0,
            External = 1,
            Download = 2,
        }

        public enum QuoteStyle
        {
            Default = 0,
            LeftAligned = 1
        }

        public enum HeadingH1H2
        {
            H1 = 1,
            H2 = 2
        }
        public enum HeadingH2H3
        {
            H2 = 2,
            H3 = 3
        }
        public enum HeadingH3H4
        {
            H3 = 3,
            H4 = 4
        }
        public enum HeadingH1H2H3
        {
            H1 = 1,
            H2 = 2,
            H3 = 3
        }
        public enum Heading
        {
            H1 = 1,
            H2 = 2,
            H3 = 3,
            H4 = 4,
            H5 = 5
        }
        public enum ListBlockStyles
        {
            Checked = 0,
            Ordered = 1,
            UnOrdered = 2,
        }
        public enum SocialChannel
        {
            FaceBook = 0,
            Twitter = 1,
            LinkedIn = 2,
            MailTo = 3
        }
        public enum Market
        {
            Consumer,
            Mkb,
            SmallBusiness,
            Grootzakelijk
        }

        public enum AlignStyle
        {
            LeftAligned = 0,
            RightAligned = 1
        }

        public enum AlignStyleHeroInspire
        {
            LeftAligned = 0,
            MiddleAligned = 1
        }

        public enum SalesBlockType
        {
            Action = 0,
            Inform = 1
        }

        public enum BackgroundColorsForUsp
        {
            White,
            Green,
            Blue
        }

        public enum KeywordType
        {
            Primary,
            Secondary,
            Tertiary,
            Quaternary,
            Quinary
        }

        public enum PageLayoutStyle
        {
            [Description("Select the layout of the page")]
            SelectLayout = 0,
            [Description("Product overview page")]
            ProductOverview = 1,
            [Description("Article cards overview page")]
            ArticleCardsOverview = 2,
            [Description("Thankyou page")]
            Thankyou = 3,
            [Description("Form page")]
            FormPage = 4
        }

        public enum VattenfallIcon
        {
            AddContact,
            Alert,
            ArrowDownLeft,
            ArrowDown,
            ArrowDownRight,
            ArrowLeft,
            ArrowRight,
            ArrowUpLeft,
            ArrowUp,
            ArrowUpRight,
            Calendar,
            Call,
            Camera,
            CentralHeating,
            Chat,
            Checkbox,
            DownloadPdf,
            Download,
            Edit,
            Elipsis,
            Eye,
            Favourite,
            Gallery,
            GuideIcon,
            Heart,
            HeatPump,
            Help,
            HomeSecurity,
            Information,
            Insulation,
            Locked,
            Mail,
            Maps,
            NinaAvatarBackground,
            NinaHappyAvatar,
            Notvisible,
            Off,
            OpenNewWindow,
            Pause,
            Phone,
            Pin,
            Play,
            PowerAndGas,
            Print,
            Settings,
            Share,
            Shop,
            SignOut,
            SolarBoiler,
            Sort,
            Tag,
            Unlocked,
            User,
            Ventilation,
            ZoomIn,
            ZoomOut,
            SustainableUseOfResources,
            Airplane,
            ApartmentHouse,
            Battery,
            BenchmarkCo2,
            BenchmarkCockpit,
            Benchmark,
            BenchmarkTemperature,
            Benefit,
            Biofuelbiomass,
            Brainstorm,
            Callback,
            Chart,
            City,
            Clock,
            Coal,
            Cold,
            CombinedHeatAndPower,
            Consumption,
            ContractDetails,
            Corporate,
            CostSaving,
            Cottage,
            Currency,
            Defence,
            DiscussionB,
            Discussion,
            Distribution,
            DistrictCooling,
            DistrictHeatAndPower,
            DistrictHeat,
            Dont,
            Ebike,
            Emobility,
            Economize,
            EkoHourlySpot,
            Electricity,
            ElectronicPaymentEuro,
            EnergyMix,
            EnergySolutions,
            Euro,
            Evaluation,
            Farm,
            Fax,
            FullWaterQuartalIcon,
            Gas,
            Globe,
            GoodDeal,
            GroupDiscussion,
            Heating,
            Hot,
            House,
            Hydro,
            InQueue,
            Industry,
            Invoices,
            LargeBattery,
            Lecture,
            LifeEvents,
            LightBulb,
            ManageMyTeam,
            Mobility,
            Modules,
            MoveWithExistingContract,
            MoveWithNewContract,
            MultiHomeBox,
            MyCases,
            MyDocuments,
            NighttimeElectricityMetering,
            Nuclear,
            OceanEnergy,
            Offshore,
            OilCondesingGasTurbine,
            Partners,
            Peat,
            PlannedOutage,
            Policies,
            PowerGrid,
            PowerPlant,
            Premium,
            Present,
            PrivateEndUser,
            Private,
            Recycling,
            SalaryTimeAndBenefits,
            SemiDetachedHouse,
            ShipDocking,
            SmartHome,
            SmartphoneText,
            SolarPanel,
            SolarPower,
            StandardContractContinuos,
            Support,
            Temperature,
            VoiceSearch,
            Wind,
            CardAndTag,
            ChargeUpYourBusiness,
            ChargingWallBox,
            ParkingMeter,
            Partner,
            PrivateCharging,
            PublicBusinessCharging,
            PublicCharging,
            Scooter2,
            Scooter,
            SmartChargingPoles,
            Check,
            Close,
            Down,
            Filter,
            Home,
            Left,
            LessInfo,
            Menu,
            More,
            Refresh,
            Right,
            Rss,
            Search,
            Send,
            Up,
            TVRemote,
            Alarm,
            Cloudy,
            CoffeeMaker,
            Console,
            Desklamp,
            Dishwasher,
            DoorClosed,
            DoorOpen,
            Dryer,
            FridgeFreezer,
            Ice,
            Kettle,
            Laptop,
            LightSwitch,
            Lightbulb,
            LightbulbStep1,
            LightbulbStep2,
            LightbulbStep3,
            LightbulbStep4,
            LightbulbStep5,
            Microwave,
            Moon,
            PhoneCharging,
            Radiator,
            Rain,
            Router,
            SmartPlug,
            SmokeAlarm,
            Snow,
            Sun,
            TableLamp2,
            TableLamp,
            Thunder,
            Ventilator,
            WashingMachine,
            WindowBlinds,
            WindowClosed,
            WindowOpen,
            Window,
            Dropbox,
            Facebook,
            Flickr,
            GooglePlus,
            Instagram,
            Linkedin,
            Pinterest,
            Skype,
            Slideshare,
            Snapchat,
            Twitter,
            Vimeo,
            Xing,
            Youtube,
            AllocateAgent,
            AllocateLocation,
            AllocateParty,
            AllocateTeam,
            Promotion,
            Checked,
            Meter,
            None,
        }

        public enum RegisterType
        {
            Register,
            ForgotUsername
        }

        public enum EngagementPhase
        {
            Undefined,
            Awareness,
            Consideration,
            Action,
            Decision,
            Onboarding,
            Retention
        }

        public enum FormInputSize
        {
            Large,
            Medium,
            Small
        }

        public enum LightBoxStyle
        {
            [Description("Simple text and link")]
            SimpleTextAndLink = 0, 
            [Description("Image with link button")]
            ImageAndLinkButton = 1,
        }
    }
}