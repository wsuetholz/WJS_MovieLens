using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.TextValidateProviders;
using WJS_MovieLens.Model;
using WJS_MovieLens.Services;

namespace WJS_MovieLens.GUI
{
    public class MovieGUI : IDisposable
    {
        NLog.Logger log = null;

        public Toplevel Top { get; set; }

        public Window Win { get; set; }

        TableView tableView;
        TableView genreView;

        User CurrentUser = null;

        string TitleFilter { get; set; }
        string OccupationFilter { get; set; }

        private bool disposedValue;

        public MovieGUI(Toplevel top)
        {
            log = NLog.LogManager.GetCurrentClassLogger();

            log.Info("MovieGUI Initializing.");

            Application.Init();

            TitleFilter = "";
            OccupationFilter = "";

            Top = top;
            if (Top == null)
            {
                Top = Application.Top;
            }

            Win = new Window($"Movie Lens Database -- CTRL-Q to Close")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var menu = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem ("_System", new MenuItem[] {
                    new MenuItem ("_Refresh", "Refresh List of Videos", () => { PopulateTables(); }),
                    new MenuItem ("_Quit", "", () => { if (Quit()) RequestStop(); })
                }),
                new MenuBarItem ("_Edit", new MenuItem[] {
                    new MenuItem ("_Add", "Add Video", () => { VideoAdd(); } ),
                    new MenuItem ("_Modify", "Modify Selected Video", () => { VideoEdit(); }),
                    new MenuItem ("_Delete", "Delete Selected Video", () => { VideoDelete(); } ),
                    new MenuItem ("_Create User", "Select a User To Use for Ratings.", () => { CreateUser(); }),
                    new MenuItem ("_Rate Movie", "Rate the Selected Movie.", () => { VideoRate(); })
                }),
                new MenuBarItem ("_Filter", new MenuItem[] {
                    new MenuItem ("_Title", "Filter on Video Title", () => { if (SetTitleFilter()) PopulateTables(); }),
                    new MenuItem ("_Occupation", "Filter on Occupation Name.", () => { if (SetOccupationFilter()) PopulateTables(); }),
                    new MenuItem ("_Clear", "Clear All Filters", () => { ClearFilters(); PopulateTables(); }),
                    new MenuItem ("_---------------------", "", null ),
                    new MenuItem ("_List Top", "Show One of The Top Rated Movies", () => { ShowTopRatedMovie(); }),
                    new MenuItem ("_Missing Occupations", "List of Occupations that HAVE NOT rated the selected movie.", () => { ShowMissingOccupations(); })
                })
            });

            var statusBar = new StatusBar(new StatusItem[] {
                new StatusItem(Key.F1, "~F1~ Help", () => Help()),
                new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => { if (Quit ()) RequestStop(); })
            });

            tableView = new TableView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
            };

            tableView.FullRowSelect = true;
            tableView.Style.ColumnStyles.Clear();
            tableView.Style.AlwaysShowHeaders = true;
            tableView.CellActivated += EditVideo;
            tableView.MaxCellWidth = 40;

            tableView.Table = new DataTable();
            tableView.Table.Columns.Add("Id", typeof(long));
            tableView.Table.Columns.Add("Title", typeof(string));
            tableView.Table.Columns.Add("Genres", typeof(string));
            tableView.Table.Columns.Add("Min", typeof(long));
            tableView.Table.Columns.Add("Avg", typeof(long));
            tableView.Table.Columns.Add("Max", typeof(long));

            PopulateTables();   // Move to a background thread...

            //var scrollBar = new ScrollBarView(tableView, true);
            //scrollBar.ChangedPosition += () =>
            //{
            //    tableView.RowOffset = scrollBar.Position;
            //    if (tableView.RowOffset != scrollBar.Position)
            //    {
            //        scrollBar.Position = tableView.RowOffset;
            //    }
            //    tableView.SetNeedsDisplay();
            //};

            //tableView.DrawContent += (e) =>
            //{
            //    scrollBar.Size = tableView.Table?.Rows?.Count ?? 0;
            //    scrollBar.Position = tableView.RowOffset;
            //    scrollBar.Refresh();
            //};

            Win.Add(tableView);

            Top.Add(Win);
            Top.Add(menu, statusBar);
        }

        public void CreateUser()
        {
            log.Info("CreateUser Method Starting.");

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Create User for Ratings", 60, 20, ok, cancel);

            var lblGender = new Label()
            {
                X = 0,
                Y = 1,
                Width = 14,
                Text = "User Gender"
            };

            var prvdGender = new TextRegexProvider("^[MFmf]$");
            var tfGender = new TextValidateField(prvdGender)
            {
                X = 20,
                Y = 1,
                Width = 20,
                TextAlignment = TextAlignment.Centered
            };

            var lblAge = new Label()
            {
                X = 0,
                Y = 2,
                Width = 14,
                Text = "User Age"
            };

            var prvdAge = new TextRegexProvider("^[0-9]?[0-9]?[0-9]$");
            var tfAge = new TextValidateField(prvdAge)
            {
                X = 20,
                Y = 2,
                Width = 20,
                TextAlignment = TextAlignment.Centered
            };

            var lblZip = new Label()
            {
                X = 0,
                Y = 3,
                Width = 14,
                Text = "User Zip Code"
            };

            var prvdZip = new TextRegexProvider("^[0-9]?[0-9]?[0-9]?[0-9]?[0-9]$");
            var tfZip = new TextValidateField(prvdZip)
            {
                X = 20,
                Y = 3,
                Width = 20,
                TextAlignment = TextAlignment.Centered
            };

            var lblOccupation = new Label()
            {
                X = 0,
                Y = 4,
                Width = 14,
                Text = "User Occupations.  Select One."
            };

            var occupationView = new TableView()
            {
                X = 0,
                Y = 5,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
            };

            occupationView.FullRowSelect = true;
            occupationView.Style.ColumnStyles.Clear();
            occupationView.Style.AlwaysShowHeaders = true;
            occupationView.MaxCellWidth = 40;

            occupationView.Table = new DataTable();
            occupationView.Table.Columns.Add("Id", typeof(long));
            occupationView.Table.Columns.Add("Occupation", typeof(string));

            using (var db = new DbMediaService())
            {
                foreach (var occupation in db.Occupations)
                {
                    DataRow row = occupationView.Table.NewRow();

                    row["Id"] = occupation.Id;
                    row["Occupation"] = occupation.Name;

                    occupationView.Table.Rows.Add(row);
                }
            }


            d.Add(lblGender, tfGender);
            d.Add(lblAge, tfAge);
            d.Add(lblZip, tfZip);
            d.Add(lblOccupation, occupationView);
            tfGender.SetFocus();

            bool dataOk = true;
            Application.Run(d);

            if (okPressed)
            {
                log.Info("CreateUser: OkPressed, Gender: {gender}, Age: {age}, Zip: {zip}, OccRow: {occrow}", tfGender.Text.ToString(), tfAge.Text.ToString(), tfZip.Text.ToString(), occupationView.SelectedRow);

                if (!tfGender.IsValid)
                {
                    MessageBox.Query(50, 7, "Error", "Gender must be M or F for Male or Female.", "Ok");
                    dataOk = false;
                }
                if (!tfAge.IsValid)
                {
                    MessageBox.Query(50, 7, "Error", "Age must be filled in ranging from 0 to 999.", "Ok");
                    dataOk = false;
                }
                if (!tfZip.IsValid)
                {
                    MessageBox.Query(50, 7, "Error", "Zip Code must be filled in ranging from 0 to 99999.", "Ok");
                    dataOk = false;
                }
                if (occupationView.SelectedRow < 0)
                {
                    MessageBox.Query(50, 7, "Error", "You must select an occupation.", "Ok");
                    dataOk = false;
                }

                if (dataOk)
                {
                    try
                    {
                        CurrentUser = new User();
                        CurrentUser.Age = Int64.Parse(tfAge.Text.ToString());
                        CurrentUser.ZipCode = tfZip.Text.ToString();
                        CurrentUser.Gender = tfGender.Text.ToString().ToUpper();
                        CurrentUser.Occupation = new Occupation();
                        CurrentUser.Occupation.Id = (long)occupationView.Table.Rows[occupationView.SelectedRow][0];
                        CurrentUser.Occupation.Name = occupationView.Table.Rows[occupationView.SelectedRow][1].ToString();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error setting up CurrentUser");
                    }
                }

                if (dataOk)
                {
                    using (var db = new DbMediaService())
                    {
                        log.Info("CreateUser: Updating Database");
                        db.Update(CurrentUser);

                        db.SaveChanges();

                        log.Info("CreateUser: Database Updated");

                        string gender = "Male";
                        if (CurrentUser.Gender.Equals("F"))
                            gender = "Female";

                        MessageBox.Query(50, 7, "Completion", $"You Added a {CurrentUser.Age} year old {gender}\nLocated in Zip Code {CurrentUser.ZipCode}\nWith Occupation {CurrentUser.Occupation.Name}\nUser Record..", "Ok");
                    }
                }
            }
            log.Info("CreateUser Method Finished.");
        }

        public bool SetTitleFilter()
        {
            var oldValue = TitleFilter;

            log.Info("SetTitleFilter Method Starting.  Current Filter \"{filter}\"", oldValue.ToString());

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Set Video Title Filter", 40, 10, ok, cancel);

            var lblT = new Label()
            {
                X = 0,
                Y = 1,
                Text = "Video Title Filter"
            };

            var tfT = new TextField()
            {
                Text = oldValue,
                X = 0,
                Y = 2,
                Width = Dim.Fill()
            };

            d.Add(lblT, tfT);
            tfT.SetFocus();

            Application.Run(d);


            if (okPressed)
            {
                TitleFilter = tfT.Text.ToString();
            }

            log.Info("SetTitleFilter Method Finishing.  Current Filter \"{filter}\"", TitleFilter.ToString());

            return okPressed;
        }

        public bool SetOccupationFilter()
        {
            var oldValue = OccupationFilter;

            log.Info("SetOccupationFilter Method Starting.  Current Filter \"{filter}\"", oldValue.ToString());

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Set Video Rating User Occupation Filter", 40, 10, ok, cancel);

            var lbl = new Label()
            {
                X = 0,
                Y = 1,
                Text = "Occupation Filter"
            };

            var tf = new TextField()
            {
                Text = oldValue,
                X = 0,
                Y = 2,
                Width = Dim.Fill()
            };

            d.Add(lbl, tf);
            tf.SetFocus();

            bool dataOk = true;

            Application.Run(d);

            if (okPressed)
            {
                OccupationFilter = tf.Text.ToString();

                if (OccupationFilter.Length > 0)
                {
                    using (var db = new DbMediaService())
                    {
                        var occupation = db.Occupations.FirstOrDefault(x => x.Name.Contains(OccupationFilter));
                        if (occupation == null)
                        {
                            MessageBox.Query(50, 7, "Error", $"The String \"{OccupationFilter}\" does not match ANY occupations.\nPlease Try Again.", "Ok");
                            dataOk = false;
                            OccupationFilter = oldValue;
                        }
                    }
                }                    
            }

            log.Info("SetOccupationFilter Method Finishing.  Current Filter \"{filter}\"", OccupationFilter.ToString());

            return (okPressed && dataOk);
        }

        public void ClearFilters()
        {
            log.Info("ClearFilters Method Called.");
            
            TitleFilter = "";
            OccupationFilter = "";
        }

        public DataRow UpdateVideoRow(Movie movie, ref DataRow row)
        {
            row["Id"] = movie.Id;
            row["Title"] = movie.Title;

            string genres = "";
            foreach (var genre in movie.MovieGenres)
            {
                if (genres.Length < 1)
                {
                    genres = genre.Genre.Name;
                }
                else
                {
                    genres = $"{genres}, {genre.Genre.Name}";
                }
            }
            row["Genres"] = genres;

            long min = 999;
            long max = 0;
            long avg = 0;

            foreach (var rating in movie.UserMovies)
            {
                avg += rating.Rating;
                if (min > rating.Rating)
                {
                    min = rating.Rating;
                }
                if (max < rating.Rating)
                {
                    max = rating.Rating;
                }
            }
            if (movie.UserMovies.Count > 0)
                avg = avg / movie.UserMovies.Count;
            else
                min = 0;
            row["Min"] = min;
            row["Max"] = max;
            row["Avg"] = avg;

            return row;
        }

        public void PopulateVideoRow(Movie movie)
        {
            DataRow row = tableView.Table.NewRow();

            UpdateVideoRow(movie, ref row);

            tableView.Table.Rows.Add(row);
        }

        public void PopulateTables()
        {
            log.Info("PopulateTables Method Starting.");

            int rowCount = 0;
            tableView.Table.Clear();
            tableView.Update();

            using (var db = new DbMediaService())
            {
                var movieList = db.Movies.Where(x => x.Title.Contains(TitleFilter))
                    .Include(x => x.MovieGenres).ThenInclude(x => x.Genre)
                    .Include(x => x.UserMovies).ThenInclude(x => x.User).ThenInclude(x => x.Occupation);

                foreach (var movie in movieList)
                {
                    bool ok = true;
                    if (movie != null && movie.UserMovies != null && OccupationFilter.Length > 0)
                    {
                        var occ = movie.UserMovies.FirstOrDefault(x => x.User.Occupation.Name.Contains(OccupationFilter));

                        if (occ == null)
                            ok = false;
                    }

                    if (ok)
                        PopulateVideoRow(movie);
                }

                if (((rowCount++) % 40) == 0)
                {
                    tableView.Update();
                }
            }
            tableView.Update();

            log.Info("PopulateTables Method Finished.");
        }

        public void VideoAdd()
        {
            log.Info("VideoAdd Method Called.");

            Movie movie = null;

            movie = AddUpdateMovie(movie, true);
            PopulateVideoRow(movie);
        }

        public void VideoEdit()
        {
            log.Info("VideoEdit Method Called.");

            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Edit.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            movie = AddUpdateMovie(movie, false);

            DataRow row = tableView.Table.Rows[tableView.SelectedRow];

            UpdateVideoRow(movie, ref row);

            tableView.Update();
        }

        public void VideoDelete()
        {
            log.Info("VideoDelete Method Called.");

            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Delete.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
                if (movie != null)
                {
                    var n = MessageBox.Query(50, 7, "Delete Movie", $"Are you sure you want to delete the Movie \"{movie.Title}\"?", "Yes", "No");
                    if (n == 0)
                    {
                        foreach (var movieGenre in movie.MovieGenres)
                        {
                            db.Remove(movieGenre);
                        }
                        foreach (var userMovie in movie.UserMovies)
                        {
                            db.Remove(userMovie);
                        }
                        db.Remove(movie);

                        db.SaveChanges();

                        tableView.Table.Rows.Remove(tableView.Table.Rows[tableView.SelectedRow]);
                    }
                }
                else
                {
                    MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                    return;
                }
            }

            tableView.Table.Rows.Remove(tableView.Table.Rows[tableView.SelectedRow]);
            tableView.Update();
        }

        public void EditVideo(TableView.CellActivatedEventArgs e)
        {
            log.Info("EditVideo Method Called.");

            long movieId = 0;
            Movie movie = null;

            if (e != null && e.Table != null)
            {
                movieId = (long)e.Table.Rows[e.Row][0];
            }

            if (movieId == 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Edit.", "Ok");
                return;
            }

            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            movie = AddUpdateMovie(movie, false);

            DataRow row = tableView.Table.Rows[e.Row];

            UpdateVideoRow(movie, ref row);

            tableView.Update();
        }

        public void VideoRate()
        {
            log.Info("VideoRate Method Called.");

            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Rate.", "Ok");
                return;
            }

            if (CurrentUser == null || CurrentUser.Occupation == null || CurrentUser.Occupation.Id <= 0)
            {
                MessageBox.Query(50, 7, "Error", "No Current User to Rate the Video With.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog($"Rate Video \"{movie.Title}\"", 50, 8, ok, cancel);

            var lblRate = new Label()
            {
                X = 0,
                Y = 1,
                Width = 14,
                Text = "Video Rating (0..5)"
            };

            var prvdRate = new TextRegexProvider("^[0-5]$");
            var tfRate = new TextValidateField(prvdRate)
            {
                X = 20,
                Y = 1,
                Width = 20,
                TextAlignment = TextAlignment.Centered
            };

            d.Add(lblRate, tfRate);
            tfRate.SetFocus();

            bool dataOk = true;
            UserMovie userMovie = new UserMovie();

            Application.Run(d);

            if (okPressed)
            {
                if (!tfRate.IsValid)
                {
                    MessageBox.Query(50, 7, "Error", "Rate must be filled in ranging from 0 to 5.", "Ok");
                    dataOk = false;
                }

                if (dataOk)
                {
                    try
                    {
                        userMovie.Rating = Int64.Parse(tfRate.Text.ToString());
                        userMovie.RatedAt = DateTime.Now;
                        userMovie.Movie = movie;
                        userMovie.User = CurrentUser;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error setting up UserMovie");
                    }
                }

                if (dataOk)
                {
                    using (var db = new DbMediaService())
                    {
                        db.Update(userMovie);

                        db.SaveChanges();

                        string gender = "Male";
                        if (CurrentUser.Gender.Equals("F"))
                            gender = "Female";

                        MessageBox.Query(50, 10, "Completion", $"A User whom is a {CurrentUser.Age} year old {gender}\nLocated in Zip Code {CurrentUser.ZipCode}\nWith Occupation {CurrentUser.Occupation.Name}\nRated the Video {movie.Title}\nWith a Rating of {userMovie.Rating}..", "Ok");
                    }
                }
            }


        }

        public void ToggleSelectedGenre(TableView.CellActivatedEventArgs e)
        {
            if (e != null && e.Table != null)
            {
                if ((bool)e.Table.Rows[e.Row][0])
                {
                    e.Table.Rows[e.Row][0] = false;
                }
                else
                {
                    e.Table.Rows[e.Row][0] = true;
                }
                genreView.Update();
            }

        }

        public Movie AddUpdateMovie(Movie movie, bool addMovie)
        {
            log.Info("AddUpdateMovie Method Called.");

            string oldValue = "";
            if (movie == null)
            {
                movie = new Movie();
                movie.Id = 0;
                movie.Title = "";
                movie.MovieGenres = new List<MovieGenre>();
                movie.UserMovies = new List<UserMovie>();
            }

            oldValue = movie.Title;

            var oldDate = movie.ReleaseDate;
            long oldRating = 0;

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Add/Modify Video Information", 60, 20, ok, cancel);

            var lblT = new Label()
            {
                X = 0,
                Y = 1,
                Text = "Video Title"
            };

            var tfT = new TextField()
            {
                Text = oldValue,
                X = 0,
                Y = 2,
                Width = Dim.Fill()
            };

            var lblR = new Label()
            {
                X = 0,
                Y = 3,
                Text = "Video Release Date"
            };

            var dfR = new DateField()
            {
                Date = oldDate,
                X = 0,
                Y = 4,
                Width = Dim.Fill()
            };

            var lblRate = new Label()
            {
                X = 0,
                Y = 5,
                Text = "Video Rating"
            };

            var tfRate = new TextField()
            {
                Text = oldRating.ToString(),
                X = 0,
                Y = 6,
                Width = Dim.Fill()
            };

            var lblG = new Label()
            {
                X = 0,
                Y = 7,
                Text = "Genres"
            };

            genreView = new TableView()
            {
                X = 0,
                Y = 8,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
            };

            genreView.FullRowSelect = true;
            genreView.Style.ColumnStyles.Clear();
            genreView.Style.AlwaysShowHeaders = true;
            genreView.CellActivated += ToggleSelectedGenre;
            genreView.MaxCellWidth = 40;

            genreView.Table = new DataTable();
            genreView.Table.Columns.Add("Selected", typeof(bool));
            genreView.Table.Columns.Add("Id", typeof(long));
            genreView.Table.Columns.Add("Genre", typeof(string));

            using (var db = new DbMediaService())
            {
                foreach (var genre in db.Genres)
                {
                    DataRow row = genreView.Table.NewRow();

                    row["Id"] = genre.Id;
                    row["Genre"] = genre.Name;
                    row["Selected"] = false;

                    foreach (var movieGenre in movie.MovieGenres)
                    {
                        if (movieGenre.Genre.Id == genre.Id)
                        {
                            row["Selected"] = true;
                            break;
                        }
                    }

                    genreView.Table.Rows.Add(row);
                }
            }

            d.Add(lblT, tfT);
            d.Add(lblR, dfR);
            if (CurrentUser != null)
                d.Add(lblRate, tfRate);
            d.Add(lblG, genreView);
            tfT.SetFocus();

            Application.Run(d);

            if (okPressed)
            {
                bool doUpdate = true;
                movie.Title = tfT.Text.ToString();
                movie.ReleaseDate = dfR.Date;

                using (var db = new DbMediaService())
                {
                    var checkTitle = db.Movies.FirstOrDefault(x => x.Title.Equals(movie.Title));
                    if (checkTitle != null) // Found a matching title
                    {
                        if (movie.Id > 0 && checkTitle.Id != movie.Id)
                        {
                            MessageBox.Query(50, 7, "Error", "Duplicate movie title found in the database.", "Ok");
                            doUpdate = false;
                        }
                    }

                }

                ICollection<MovieGenre> newMovieGenres = movie.MovieGenres;
                ICollection<UserMovie> newUserMovies = movie.UserMovies;

                using (var db = new DbMediaService())
                {
                    if (doUpdate && addMovie)
                    {
                        db.Add(movie);
                        db.SaveChanges();
                        var newMovie = db.Movies.FirstOrDefault(x => x.Title.Equals(movie.Title));
                        movie.Id = newMovie.Id;
                        foreach (DataRow row in genreView.Table.Rows)
                        {
                        
                            bool selected = (bool)row[0];
                            long genreId = (long)row[1];
                            if (selected)
                            {
                                var movieGenre = new MovieGenre();
                                movieGenre.Movie = movie;
                                movieGenre.Genre = db.Genres.FirstOrDefault(x => x.Id == genreId);

                                if (movieGenre.Genre != null)
                                {
                                    db.Add(movieGenre);
                                    newMovieGenres.Add(movieGenre);
                                }
                            }
                        }
                        if (CurrentUser != null)
                        {
                            UserMovie userMovie = new UserMovie();
                            userMovie.Movie = movie;
                            userMovie.RatedAt = DateTime.Now;
                            long rating = Int64.Parse(tfRate.Text.ToString());
                            if (rating > 5)
                                rating = 5;
                            else if (rating < 0)
                                rating = 0;
                            userMovie.Rating = rating;
                            db.Add(userMovie);

                            newUserMovies.Add(userMovie);
                        }
                    }
                    else if (doUpdate)
                    {
                        foreach (DataRow row in genreView.Table.Rows)
                        {

                            bool selected = (bool)row[0];
                            long genreId = (long)row[1];
                            bool found = false;
                            foreach (var mGenre in movie.MovieGenres)
                            {
                                if (mGenre.Genre.Id == genreId)
                                {
                                    found = true;
                                    if (!selected)
                                    {
                                        mGenre.Movie = null;
                                        mGenre.Genre = null;
                                        db.Remove<MovieGenre>(mGenre);
                                        newMovieGenres.Remove(mGenre);
                                    }
                                    else
                                    {
                                        newMovieGenres.Add(mGenre);
                                    }
                                }
                            }

                            if (!found && selected)
                            {
                                var movieGenre = new MovieGenre();
                                movieGenre.Movie = movie;
                                movieGenre.Genre = db.Genres.FirstOrDefault(x => x.Id == genreId);

                                if (movieGenre.Genre != null)
                                {
                                    db.Add(movieGenre);

                                    newMovieGenres.Add(movieGenre);
                                }
                            }
                        }

                        db.SaveChanges();

                        db.Update(movie);

                    }

                    db.SaveChanges();

                    movie.MovieGenres = newMovieGenres;
                    movie.UserMovies = newUserMovies;

                }
            }

            return movie;
        }

        public void ShowTopRatedMovie()
        {
            log.Info("ShowTopRatedMovie Method Called.");

            long minAge;
            long maxAge;
            Occupation occupation;

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Select Age Bracket or Occupation", 60, 40, ok, cancel);

            var lblAge = new Label()
            {
                X = 0,
                Y = 1,
                Width = 14,
                Text = "Age Bracket.  Select One."
            };

            var ageView = new TableView()
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill() - 1,
                Height = 10,
            };

            ageView.FullRowSelect = true;
            ageView.Style.ColumnStyles.Clear();
            ageView.Style.AlwaysShowHeaders = true;
            ageView.MaxCellWidth = 40;

            ageView.Table = new DataTable();
            ageView.Table.Columns.Add("Description", typeof(string));
            ageView.Table.Columns.Add("Min Age", typeof(long));
            ageView.Table.Columns.Add("Max Age", typeof(long));

            DataRow row = ageView.Table.NewRow();

            row["Description"] = "*ALL AGES*";
            row["Min Age"] = 0;
            row["Max Age"] = 999;

            ageView.Table.Rows.Add(row);

            for (minAge = 0; minAge < 100; minAge = minAge + 20)
            {
                row = ageView.Table.NewRow();

                row["Description"] = $"{minAge} .. {minAge + 20}";
                row["Min Age"] = minAge;
                row["Max Age"] = minAge + 19;

                ageView.Table.Rows.Add(row);
            }

            var lblOccupation = new Label()
            {
                X = 0,
                Y = 12,
                Width = 14,
                Text = "User Occupations.  Select One."
            };

            var occupationView = new TableView()
            {
                X = 0,
                Y = 13,
                Width = Dim.Fill() - 1,
                Height = 20,
            };

            occupationView.FullRowSelect = true;
            occupationView.Style.ColumnStyles.Clear();
            occupationView.Style.AlwaysShowHeaders = true;
            occupationView.MaxCellWidth = 40;

            occupationView.Table = new DataTable();
            occupationView.Table.Columns.Add("Id", typeof(long));
            occupationView.Table.Columns.Add("Occupation", typeof(string));

            row = occupationView.Table.NewRow();

            row["Id"] = -1;
            row["Occupation"] = "*All Occupations*";

            occupationView.Table.Rows.Add(row);

            using (var db = new DbMediaService())
            {
                foreach (var occ in db.Occupations)
                {
                    row = occupationView.Table.NewRow();

                    row["Id"] = occ.Id;
                    row["Occupation"] = occ.Name;

                    occupationView.Table.Rows.Add(row);
                }
            }


            d.Add(lblAge, ageView);
            d.Add(lblOccupation, occupationView);
            ageView.SetFocus();

            Application.Run(d);

            occupation = null;
            minAge = 0;
            maxAge = 999;
            if (okPressed)
            {
                if (ageView.SelectedRow >= 0)
                {
                    minAge = (long)ageView.Table.Rows[ageView.SelectedRow][1];
                    maxAge = (long)ageView.Table.Rows[ageView.SelectedRow][2];
                }
                if (occupationView.SelectedRow >= 0)
                {
                    occupation = new Occupation();
                    occupation.Id = (long)occupationView.Table.Rows[occupationView.SelectedRow][0];
                    occupation.Name = occupationView.Table.Rows[occupationView.SelectedRow][1].ToString();
                }

                using (var db = new DbMediaService())
                {
                    var userMovies = db.UserMovies.Include(x => x.User).Include(x => x.User.Occupation)
                        .Where(x => ((x.User.Age >= minAge && x.User.Age <= maxAge) && (occupation == null || (occupation != null && x.User.Occupation.Id == occupation.Id))))
                        .Include(x => x.Movie);
                    if (userMovies != null)
                    {
                        var userMovieRating = userMovies
                            .OrderByDescending(x => x.Rating)
                            .FirstOrDefault();
                        if (userMovieRating != null)
                        {
                            var rating = userMovieRating.Rating;
                            var userMovie = userMovies
                                .Where(x => x.Rating == rating)
                                .OrderBy(x => x.Movie.Title)
                                .FirstOrDefault();

                            if (userMovie != null)
                            {
                                MessageBox.Query(50, 7, "Completion", $"Video {userMovie.Movie.Title} has a Rating of {userMovie.Rating}.", "Ok");
                            }
                        }
                    }
                }
            }


        }

        public void ShowMissingOccupations()
        {
            log.Info("ShowMissingOccupations Method Called.");

            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Check for Missing Occupations.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            IList<Occupation> missing = new List<Occupation>();
            using (var db = new DbMediaService())
            {
                missing = db.Occupations.ToList();

                var userMovies = db.UserMovies.Where(x => x.Movie.Id == movieId).Include(x => x.User).Include(x => x.User.Occupation);

                foreach (var userMovie in userMovies)
                {
                    if (userMovie != null && userMovie.User != null && userMovie.User.Occupation != null)
                    {
                        foreach (var occupation in missing)
                        {
                            if (occupation.Id == userMovie.User.Occupation.Id)
                            {
                                missing.Remove(occupation);
                                break;
                            }
                        }
                    }
                }

            }

            if (missing.Count > 0)
            {
                bool okPressed = false;

                var ok = new Button("Ok", is_default: true);
                ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
                var cancel = new Button("Cancel");
                cancel.Clicked += () => { Application.RequestStop(); };

                var d = new Dialog($"Results for Video \"{movie.Title}\".", 60, 20, ok, cancel);

                var lblOccupation = new Label()
                {
                    X = 0,
                    Y = 1,
                    Width = 14,
                    Text = "List of Occupations which have not rated video."
                };

                var occupationView = new TableView()
                {
                    X = 0,
                    Y = 2,
                    Width = Dim.Fill() - 1,
                    Height = Dim.Fill() - 1,
                };

                occupationView.FullRowSelect = true;
                occupationView.Style.ColumnStyles.Clear();
                occupationView.Style.AlwaysShowHeaders = true;
                occupationView.MaxCellWidth = 40;

                occupationView.Table = new DataTable();
                occupationView.Table.Columns.Add("Id", typeof(long));
                occupationView.Table.Columns.Add("Occupation", typeof(string));

                foreach (var occ in missing)
                {
                    var row = occupationView.Table.NewRow();

                    row["Id"] = occ.Id;
                    row["Occupation"] = occ.Name;

                    occupationView.Table.Rows.Add(row);
                }

                d.Add(lblOccupation, occupationView);
                occupationView.SetFocus();

                Application.Run(d);

                if (okPressed)
                {
                }
            }
            else
            {
                MessageBox.Query(50, 7, $"Results for Video \"{movie.Title}\".", "\nThere are no known occupations that have not rated this video.", "Ok");
            }
        }

        public bool Quit()
        {
            log.Info("Quit Method Called.");

            var n = MessageBox.Query(50, 7, "Quit Movie Lens Program", "Are you sure you want to quit Movie Lens?", "Yes", "No");
            return n == 0;
        }

        public void Help()
        {
            MessageBox.Query(50, 7, "Help", "This is a small help\nBe kind.", "Ok");
        }

        public void Load()
        {
            MessageBox.Query(50, 7, "Load", "This is a small load\nBe kind.", "Ok");
        }

        public void Save()
        {
            MessageBox.Query(50, 7, "Save", "This is a small save\nBe kind.", "Ok");
        }

        public void Run()
        {
            
            Application.Run(Top);
        }

        public void RequestStop()
        {
            Application.RequestStop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MovieGUI()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
