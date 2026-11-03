<!--
  Copy of the Fellbagger privacy policy, rendered at /fellbagger/privacy.

  The canonical version lives in the wainwrights-tracker repo at docs/1-privacy-policy.md.
  Edit it there first, then copy the result here and update Privacy.razor to match.
-->

# Fellbagger privacy policy

**Last updated:** 3 November 2026

Fellbagger is made by Fellside Digital ("we", "us"). This policy explains what the app does with
information about you. The short version: almost everything stays on your phone, and we have
designed it that way on purpose.

If you have any questions, email **oliver.lewington@fellsidedigital.co.uk**.

## Who is responsible

Fellside Digital, Cumbria, United Kingdom, is the data controller for anything described here.
Contact: oliver.lewington@fellsidedigital.co.uk.

## What stays on your phone

Fellbagger has no accounts of its own. Unless you choose to connect Strava or Garmin Connect
(both described below), there is no sign-in at all. The following is stored only on your device
and is never sent to us:

- Your ascent log — which fells you have bagged and when
- Your recorded walks, including the GPS track of each walk
- Your settings, and whether you have bought Fellbagger Pro
- The app's diagnostics log (used only if you choose to copy it and send it to us yourself)

Because we never receive this information, we cannot see it, recover it, or delete it for you.
**Deleting the app deletes all of it.** You can export any walk or route as a GPX file at any time
if you want a copy.

## Location

The app uses your phone's location to:

- Show where you are on the map and how far you are from each fell
- Record the track of a walk while you are recording one
- Bag summits automatically as you reach them (a Fellbagger Pro feature)
- Show the weather where you are, and sunsets, clear nights and other outdoor events near you

While you are recording a walk, the app keeps using your location with your phone in your
pocket or the screen off, so the recording continues and summits are still detected. It only does
this for a walk you started, and it stops when you end the walk. The app never asks for location
"all the time" and never uses your location when you aren't recording or using it. Where your
phone has a barometer, the app also reads it on the phone to measure ascent more accurately than
GPS alone.

**Your precise location stays on your phone.** It is never sent to us, and walks you record in
Fellbagger, with their GPS tracks, never leave your phone. Activities you choose to bring in from
Garmin Connect work differently and reach your phone through our server, as described under
*Connecting Garmin Connect*. To show the weather where you are and outdoor events near you, the
app sends an **approximate** position — rounded to about 1 km — to the weather service
(Open-Meteo, below), and your phone's own place-name lookup (Apple's or Google's) uses the same
approximate position to show the name of the nearest town. Nothing sent with it identifies you.
For event alerts, the app remembers the approximate position it last used, on your phone, so it
can check the forecast there later; it never tracks where you are in the background for this.

You can turn location access off at any time in your phone's settings; the app still works for
logging fells by hand.

## What leaves your phone, and where it goes

Some features need the internet. Each is listed below with exactly what is sent. Apart from the
Strava and Garmin connections you choose to make, none of them includes your name, an account, or
your ascent history.

| Sent to | What is sent | Why |
|---|---|---|
| **Open-Meteo** (open-meteo.com) | The coordinates and height of a *fell*; and, for the weather where you are and events near you, your approximate position (rounded to about 1 km) and points about 50 km either side of it | Mountain and local weather forecasts |
| **Apple or Google** (your phone's built-in place lookup) | Your approximate position (rounded to about 1 km) | The name of the nearest town |
| **NOAA Space Weather Prediction Center** (swpc.noaa.gov) | Nothing about you: a request for its public three-day aurora forecast | The northern lights chance |
| **Our content server** (api.fellbagger.app) | A version number of the content you already have | Updates to fell descriptions and routes |
| **Our image server** (images.fellbagger.app) | The filename of a fell photo | Fell photography |
| **OpenStreetMap, OpenTopoMap, Ordnance Survey and Mapbox** | Requests for map tiles, which reveal roughly which area of the map you are looking at | Map display |
| **Sentry** (sentry.io) | Crash reports: what went wrong in the app, your phone model, operating system version and app version | Finding and fixing crashes |
| **Our server** (api.fellbagger.app) — only if you connect Strava | The sign-in code or refresh token Strava issues, passed straight to Strava with our app secret and not kept | Signing in to Strava |
| **Strava** — only if you connect Strava | Requests for your activities; and, if you turn on fell lines, the line added to a post you approve | Bringing your walks in, and adding fell lines |
| **Our server** (api.fellbagger.app) and **Garmin** — only if you connect Garmin Connect | See *Connecting Garmin Connect* below | Bringing your Garmin activities in |
| **Our content server** (api.fellbagger.app) — only when you send a report | What you write in *Report a problem* or *Suggest a feature*, your email address if you choose to give one, and the app version, phone model and operating system version | Fixing problems you report, and replying if you asked us to |

Like any internet request, these carry your phone's IP address to the service receiving them.
Our own servers keep standard, short-lived access logs (IP address, time, what was requested) for
security and to see how much capacity we need. Crash reports never include your location, your
walks, or your ascent log, and are kept by Sentry for 30 days. Map tiles are cached on your phone
so that the maps work offline and the same area is not requested again.

Reports you send from Settings → Help & Support are only sent when you press Send, and the page
shows you exactly what goes with them first. Giving an email address is optional; we use it only
to reply to that report. We keep reports until they are dealt with and for at most 12 months
after that, and you can ask us to delete one at any time.

Open-Meteo, OpenStreetMap, OpenTopoMap, Ordnance Survey and Mapbox each have their own privacy
policies covering the requests they receive. Open-Meteo keeps its web server logs, which can
include the coordinates asked about, for up to 90 days to troubleshoot its service, and does not
share them. Mapbox's own usage-analytics feature (which would
otherwise report your approximate location to Mapbox) is switched off in the app; only ordinary
map-tile requests are made, the same as the other map providers above.

Our server (api.fellbagger.app) runs on **Railway** (Railway Corporation, United States), which
hosts the server and its database for us as our processor. Fell photographs and terrain data are
served through **Cloudflare**.

## Connecting Strava

Connecting Strava is optional. Strava's sign-in screen sets out what you are allowing. Your Strava
sign-in tokens are kept in your phone's secure storage. Your activities are downloaded directly
from Strava to your phone, and the walks and tracks you bring in are stored only there. Our server
only completes the sign-in, as listed above, and keeps nothing from it. If you turn on fell lines,
the app writes a line to the Strava posts you approve, and only those.

Disconnecting in the app removes the tokens from your phone. Walks you have already brought in
stay on your phone until you delete them. Strava's handling of your data is covered by its own
privacy policy.

## Connecting Garmin Connect

Connecting Garmin Connect is optional, needs Fellbagger Pro, and only happens if you agree on
Garmin's own consent screen. It works differently from everything else in this policy, because
Garmin delivers activities to a server rather than to a phone. **This is the one case where your
activity data passes through us.**

**What we receive.** Once you connect, Garmin sends our server each new activity you sync to
Garmin Connect, including outdoor activities and any others on your account. That data is linked
to your Garmin user ID, and can include the GPS track, times, distance, ascent, activity type, and
whatever else your device recorded, such as heart rate. We also hold the Garmin user ID and the
connection credentials Garmin issues for you, encrypted, so that we can keep your connection
working and end it when you ask.

**How we use it.** Only to deliver your activities to your Fellbagger app, where the app looks for
the fells you climbed so that you can review them and log them. We use the track, times, distance,
ascent and activity type. We do not use heart rate or other health measurements for anything. We
do not look at your activities ourselves, except where you ask us to help with a problem or where
it is needed to keep the service secure and working.

**How long we keep it.** Each activity stays on our server only until your phone has collected it,
and is then deleted from the server. Anything not collected within 30 days is deleted anyway. If
you disconnect, we delete everything waiting for you. If your app collects nothing for 60 days, we end
the Garmin connection and delete what we hold; you can
connect again at any time. Activities you have collected are stored only on your phone.

**Who else receives it.** Garmin data on our server is held with Railway, our hosting provider,
as our processor. It is not sent to any other company. It is not included in crash reports, and it
does not go to Sentry, RevenueCat or any map or weather provider. Your Garmin data is not sold
or shared with anyone, and it is not used for advertising.

**AI.** No AI system processes your Garmin data, and it is never used to train, evaluate or improve
AI models.

**Your control.** You can withdraw consent at any time by disconnecting in the app, or by removing
Fellbagger from Connected Apps in your Garmin Connect account; either ends delivery and deletes
what is waiting on our server. Collected walks are on your phone, where you can see them, correct
which fells were logged, delete them, or export any of them as a GPX file. To ask what we hold or
to have it deleted, email oliver.lewington@fellsidedigital.co.uk.

Garmin handles your Garmin account under its own privacy policy. Garmin and Garmin Connect are
trademarks of Garmin Ltd. or its subsidiaries. Fellbagger is not made by or affiliated with Garmin.

## Purchases

Fellbagger Pro is sold through the Apple App Store or Google Play as a yearly or monthly
subscription, or as a one-off lifetime purchase. Apple or Google handle the payment and hold your
payment details; we never see them.

To check that a purchase is genuine and whether a subscription is still active, the app uses
**RevenueCat** (RevenueCat, Inc.) as our processor. RevenueCat receives a random identifier
created by the app (not your name, email or Apple/Google account), your purchase and subscription
history for Fellbagger, and basic device information (platform, app version, country of your
store account). It uses this only to validate purchases for us. See RevenueCat's privacy policy at
revenuecat.com/privacy. The app also keeps a copy of your Pro status on your phone so that Pro
works offline on the hill.

If a subscription ends, Pro features lock again; nothing you have logged is ever deleted.

## Notifications

Summit notifications and outdoor event alerts (for example "Sunset worth catching") are generated
and scheduled on your phone. Event alerts are off until you switch them on, and you can switch
them off at any time in the app's Settings. To keep them up to date, the app checks the forecast in
the background from time to time, using the approximate position described under Location. The
app does not use push notifications and does not send a notification token to any server.

## Children

There is no minimum age for using Fellbagger. Unless you choose to connect Strava or Garmin
Connect, which need accounts of their own, the app collects no personal information that requires
consent. It shows no advertising. Any future feature that involves sharing photos or
other content with us will be open only to people aged 16 or over, and will have its own clear
consent step.

## The legal basis for what we do

Under UK data protection law we rely on:

- **Performance of a contract** — providing the app you have installed, including the weather,
  map and content features above
- **Consent** — receiving your activities from Garmin Connect, which you give on Garmin's consent
  screen and can withdraw at any time as described above
- **Legitimate interests** — crash reporting, so that we can keep the app working; short-lived
  server logs for security; and reading the problem reports and suggestions you choose to send

We do not sell information about you, we do not use advertising or tracking SDKs, and we do not
use analytics that profile you.

## Your rights

Because we hold almost nothing about you, most rights are exercised simply by managing the app on
your own phone. The exception is Garmin activities waiting on our server, covered under
*Connecting Garmin Connect*. You still have the right to ask us what we hold, to have it corrected or deleted,
to object to our use of it, and to complain to the Information Commissioner's Office
(ico.org.uk). Email oliver.lewington@fellsidedigital.co.uk and we will respond within one month.

## Hill data

Fell and summit data in the app comes from the Database of British and Irish Hills
(hill-bagging.co.uk/dobih), used under the CC BY 4.0 licence. This is about the hills, not you.

## Changes to this policy

If we change what the app does with your information, we will update this page and the "last
updated" date, and describe the change in the app's release notes. Continuing to use the app
after a change means the new policy applies.
