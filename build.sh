# curl -sSL https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.sh | DNX_BRANCH=dev sh 
source ~/.dnx/dnvm/dnvm.sh
dnvm list # should return blank list
dnu restore
dnu build