using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentScheduler;
using System.Data.Entity.Infrastructure;
using IEP_Petar.Models;

namespace IEP_Petar.Hubs
{
    public class AuctionsHub : Hub
    {
        public void getTimes()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var auctions = from a in db.Auctions select a;
            var auctionList = auctions.ToList();

            foreach (var auction in auctionList)
            {
                DateTime now = DateTime.Now;
                DateTime end = auction.ClosingTime;
                TimeSpan diff = new TimeSpan(0);

                if (auction.State == 3 && now <= end)
                {
                    diff = end.Subtract(now);
                }

                Clients.Caller.sendDuration(auction.AuctionId, (long)diff.TotalSeconds);
            }
        }

        public void getTime(string AuctionID)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            Auction auction = db.Auctions.Find(Convert.ToInt64(AuctionID));
            DateTime now = DateTime.Now;
            DateTime end = auction.ClosingTime;
            TimeSpan diff = new TimeSpan(0);
            if (now <= end) diff = end.Subtract(now);
            Clients.Caller.sendSeconds((long)diff.TotalSeconds);
        }

        public void bid(string AuctionID, string UserID, string rv)
        {
            {
                ApplicationDbContext db = new ApplicationDbContext();
                DateTime now = DateTime.Now;
                ApplicationUser lastBidder = db.Users.Find(UserID); //user which is trying to bid                
                Auction auction = db.Auctions.Find(Convert.ToInt64(AuctionID));
                ApplicationUser previousBidder = auction.ApplicationUserID == null ? null : db.Users.Find(auction.ApplicationUserID);  //user which had a bid before this needs to get his tokens back
                byte[] rowVersion = Convert.FromBase64String(rv);
                bool success = false; bool tenSecondRule = false; bool notEnoughTokens = false; bool timeUp = false; bool concIssue = false;

                if (auction.ClosingTime <= now) timeUp = true;
                else
                {
                    int tokensToBid = (auction.CurrentPrice / 50) + 1; //Price + 1 token for a bid;
                    if (previousBidder != null)
                    {
                        if (previousBidder.Id != lastBidder.Id)
                        { 
                            previousBidder.Tokens += (auction.CurrentPrice / 50) - 1;
                        }
                        else
                        {
                            tokensToBid = 2;    //1 for the price increase, 1 for the bid price
                        }
                    }                   
                    if (lastBidder.Tokens > tokensToBid)
                    {
                        //update auction's current price and last bidder                    
                        auction.CurrentPrice += 50; //za 50 dinara se povecava cena
                        auction.ApplicationUserID = UserID;
                        auction.ApplicationUser = lastBidder;

                        //10 second rule
                        TimeSpan ts = auction.ClosingTime.Subtract(now);
                        if ((ts.TotalSeconds > 0) && (ts.TotalSeconds < 10))
                        {
                            auction.Duration += 10 - (long)ts.TotalSeconds;
                            DateTime openingTime = auction.OpeningTime;
                            auction.ClosingTime = openingTime.AddSeconds(auction.Duration);
                            tenSecondRule = true;
                        }

                        try
                        {
                            db.Entry(auction).OriginalValues["RowVersion"] = rowVersion;
                            db.SaveChanges();
                            //make new bid
                            Bid bid = new Bid();

                            //update last bidder's tokens
                            lastBidder.Tokens -= tokensToBid;
                            db.Entry(lastBidder).State = System.Data.Entity.EntityState.Modified;

                            //If there was a previous bidder, give him back his tokens
                            if(previousBidder != null)
                            {
                                db.Entry(previousBidder).State = System.Data.Entity.EntityState.Modified;
                            }

                            //save amount, bidder, auction and time in bid
                            bid.Amount = auction.CurrentPrice;
                            bid.ApplicationUser = lastBidder;
                            bid.Auction = auction;
                            bid.Time = now;

                            db.Bids.Add(bid);
                            db.SaveChanges();
                            success = true;
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            concIssue = true;
                        }
                    }
                    else
                    {
                        notEnoughTokens = true;
                    }
                }
                if (success)
                {
                    var newRowVersion = Convert.ToBase64String(auction.RowVersion);
                    Clients.All.newBid(AuctionID, lastBidder.Name + " " + lastBidder.Surname, auction.CurrentPrice, newRowVersion);
                    if (tenSecondRule) Clients.All.tenSecondRule(AuctionID);
                }
                else
                {
                    if (notEnoughTokens) Clients.Caller.unsuccessfulBid();
                    else
                    {
                        if (timeUp) Clients.Caller.timeUp();
                        if (concIssue) Clients.Caller.concurrencyIssue();
                    }
                }
            }
        }
    }
    public class MyRegistry : Registry
    {
        public MyRegistry()
        {
            Schedule(() =>
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<AuctionsHub>();

                ApplicationDbContext db = new ApplicationDbContext();
                var auctions = from a in db.Auctions select a;
                auctions = auctions.Where(s => s.State == 3);
                var auctionList = auctions.ToList();

                foreach (var auction in auctionList)
                {
                    DateTime now = DateTime.Now;
                    DateTime end = auction.ClosingTime;
                    TimeSpan diff = new TimeSpan(0);

                    var auctionToChange = db.Auctions.Find(auction.AuctionId);

                    if (now > end)
                    {
                        var bids = from b in db.Bids select b;
                        bids = bids.Where(s => s.AuctionId == auctionToChange.AuctionId).OrderBy(b => b.Time);
                        var bidList = bids.ToList();

                        if (bidList.Count() == 0) auctionToChange.State = 5;
                        else auctionToChange.State = 4;

                        db.Entry(auctionToChange).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        String time = diff.ToString(@"dd\:hh\:mm\:ss");
                        String newState = null;

                        switch (auctionToChange.State)
                        {
                            case 3: newState = "OPEN"; break;
                            case 4: newState = "SOLD"; break;
                            case 5: newState = "EXPIRED"; break;
                        }

                        hubContext.Clients.All.tickTime(auctionToChange.AuctionId, time, newState);
                    }
                    else
                    {
                        diff = end.Subtract(now);
                    }
                }
            }).ToRunNow().AndEvery(1).Seconds();
        }
    }
}